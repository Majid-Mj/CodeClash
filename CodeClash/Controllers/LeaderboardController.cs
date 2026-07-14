using CodeClash.Application.Common.Interfaces;
using CodeClash.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/leaderboard")]
[Authorize]
[Produces("application/json")]
public class LeaderboardController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public LeaderboardController(IApplicationDbContext context)
    {
        _context = context;
    }

    // ─── DTOs ──────────────────────────────────────────────────────────────────

    public record LeaderboardUserDto(
        Guid Id,
        string Username,
        int TotalPoints,
        int Rating,
        string FavLanguage,
        int Battles,
        int Wins,
        int Losses
    );

    public record MyStatsDto(
        Guid Id,
        string Username,
        int TotalPoints,
        int Rating,
        int Rank,
        int TotalPlayers,
        int Battles,
        int Wins,
        int Losses,
        string FavLanguage,
        Dictionary<string, int> LanguageBreakdown
    );

    // ─── GET /api/v1/leaderboard ──────────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LeaderboardUserDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] string? language = null,
        [FromQuery] string? scope = null,
        CancellationToken ct = default)
    {
        var cutoff = scope == "weekly" ? DateTime.UtcNow.AddDays(-7) : DateTime.MinValue;

        List<Guid>? qualifiedIds = null;

        if (scope == "weekly" || !string.IsNullOrWhiteSpace(language))
        {
            var subQuery = _context.Submissions.AsNoTracking();

            if (scope == "weekly")
                subQuery = subQuery.Where(s => s.CreatedAt >= cutoff);

            if (!string.IsNullOrWhiteSpace(language))
            {
                var lang = language.Trim().ToLowerInvariant();
                subQuery = subQuery.Where(s => s.Language == lang);
            }

            qualifiedIds = await subQuery
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync(ct);
        }

        var usersQuery = _context.Users.AsNoTracking().Where(u => u.IsActive);
        if (qualifiedIds != null)
            usersQuery = usersQuery.Where(u => qualifiedIds.Contains(u.Id));

        var users = await usersQuery
            .OrderByDescending(u => u.TotalPoints)
            .ThenByDescending(u => u.Rating)
            .ThenBy(u => u.Username)
            .Take(100)
            .ToListAsync(ct);

        if (users.Count == 0)
            return Ok(Array.Empty<LeaderboardUserDto>());

        var userIds = users.Select(u => u.Id).ToList();
        var enriched = await EnrichUsersAsync(userIds, ct);
        return Ok(BuildDtos(users, enriched));
    }

    // ─── GET /api/v1/leaderboard/me ───────────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MyStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyStats(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user == null) return NotFound();

        var rank = await _context.Users.AsNoTracking()
            .CountAsync(u => u.IsActive && u.TotalPoints > user.TotalPoints, ct) + 1;

        var totalPlayers = await _context.Users.AsNoTracking()
            .CountAsync(u => u.IsActive, ct);

        var battleParticipations = await _context.BattleParticipants
            .AsNoTracking()
            .Where(bp => bp.UserId == userId)
            .Select(bp => new { bp.BattleId, bp.Battle.WinnerId })
            .ToListAsync(ct);

        var totalBattles = battleParticipations.Count;
        var wins = battleParticipations.Count(bp => bp.WinnerId == userId);

        var langGroups = await _context.Submissions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .GroupBy(s => s.Language)
            .Select(g => new { Language = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var langBreakdown = langGroups
            .ToDictionary(g => CapitaliseLang(g.Language), g => g.Count);

        var favLang = langGroups.Count > 0
            ? CapitaliseLang(langGroups.OrderByDescending(g => g.Count).First().Language)
            : "N/A";

        return Ok(new MyStatsDto(
            Id: user.Id,
            Username: user.Username,
            TotalPoints: user.TotalPoints,
            Rating: user.Rating,
            Rank: rank,
            TotalPlayers: totalPlayers,
            Battles: totalBattles,
            Wins: wins,
            Losses: totalBattles - wins,
            FavLanguage: favLang,
            LanguageBreakdown: langBreakdown
        ));
    }

    // ─── GET /api/v1/leaderboard/recent-battles ──────────────────────────────
    [HttpGet("recent-battles")]
    [Authorize]
    [ProducesResponseType(typeof(LeaderboardUserDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentBattleOpponents(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var myBattleIds = await _context.BattleParticipants
            .AsNoTracking()
            .Where(bp => bp.UserId == userId)
            .Select(bp => bp.BattleId)
            .ToListAsync(ct);

        if (myBattleIds.Count == 0)
            return Ok(Array.Empty<LeaderboardUserDto>());

        var opponentIds = await _context.BattleParticipants
            .AsNoTracking()
            .Where(bp => myBattleIds.Contains(bp.BattleId) && bp.UserId != userId)
            .Select(bp => bp.UserId)
            .Distinct()
            .ToListAsync(ct);

        if (opponentIds.Count == 0)
            return Ok(Array.Empty<LeaderboardUserDto>());

        var opponents = await _context.Users
            .AsNoTracking()
            .Where(u => opponentIds.Contains(u.Id) && u.IsActive)
            .OrderByDescending(u => u.TotalPoints)
            .ThenByDescending(u => u.Rating)
            .Take(50)
            .ToListAsync(ct);

        var enriched = await EnrichUsersAsync(opponentIds.ToList(), ct);
        return Ok(BuildDtos(opponents, enriched));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private record UserEnrichment(string FavLanguage, int Battles, int Wins, int Losses);

    private async Task<Dictionary<Guid, UserEnrichment>> EnrichUsersAsync(
        List<Guid> userIds, CancellationToken ct)
    {
        var langGroups = await _context.Submissions
            .AsNoTracking()
            .Where(s => userIds.Contains(s.UserId))
            .GroupBy(s => new { s.UserId, s.Language })
            .Select(g => new { g.Key.UserId, g.Key.Language, Count = g.Count() })
            .ToListAsync(ct);

        var favLanguages = langGroups
            .GroupBy(g => g.UserId)
            .ToDictionary(
                g => g.Key,
                g => CapitaliseLang(g.OrderByDescending(x => x.Count).First().Language)
            );

        var battleData = await _context.BattleParticipants
            .AsNoTracking()
            .Where(bp => userIds.Contains(bp.UserId))
            .Select(bp => new { bp.UserId, bp.BattleId, bp.Battle.WinnerId })
            .ToListAsync(ct);

        var battleByUser = battleData
            .GroupBy(b => b.UserId)
            .ToDictionary(g => g.Key, g => new
            {
                Total = g.Count(),
                Wins = g.Count(b => b.WinnerId == b.UserId)
            });

        var result = new Dictionary<Guid, UserEnrichment>();
        foreach (var uid in userIds)
        {
            var lang = favLanguages.TryGetValue(uid, out var l) ? l : "N/A";
            var total = battleByUser.TryGetValue(uid, out var b) ? b.Total : 0;
            var wins = battleByUser.TryGetValue(uid, out var b2) ? b2.Wins : 0;
            result[uid] = new UserEnrichment(lang, total, wins, total - wins);
        }
        return result;
    }

    private static LeaderboardUserDto[] BuildDtos(
        IEnumerable<CodeClash.Domain.Entities.User> users,
        Dictionary<Guid, UserEnrichment> enriched)
    {
        return users.Select(user =>
        {
            enriched.TryGetValue(user.Id, out var e);
            return new LeaderboardUserDto(
                Id: user.Id,
                Username: user.Username,
                TotalPoints: user.TotalPoints,
                Rating: user.Rating,
                FavLanguage: e?.FavLanguage ?? "N/A",
                Battles: e?.Battles ?? 0,
                Wins: e?.Wins ?? 0,
                Losses: e?.Losses ?? 0
            );
        }).ToArray();
    }

    private static string CapitaliseLang(string lang) => lang switch
    {
        "python"     => "Python",
        "javascript" => "JavaScript",
        "csharp"     => "C#",
        "java"       => "Java",
        "cpp"        => "C++",
        _ => lang.Length > 0 ? char.ToUpperInvariant(lang[0]) + lang[1..] : lang
    };
}

