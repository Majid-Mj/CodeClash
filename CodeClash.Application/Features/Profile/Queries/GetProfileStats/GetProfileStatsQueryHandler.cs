using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Profile.DTOs;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Features.Profile.Queries.GetProfileStats;

public class GetProfileStatsQueryHandler : IRequestHandler<GetProfileStatsQuery, Result<ProfileStatsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProfileStatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProfileStatsDto>> Handle(GetProfileStatsQuery request, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user == null)
            return Result<ProfileStatsDto>.Failure("User not found.", "User not found.");

        // 1. Calculate Problems Solved (Count of unique Accepted submissions)
        var problemsSolved = await _context.Submissions
            .Where(s => s.UserId == request.UserId && s.Status == SubmissionStatus.Accepted)
            .Select(s => s.ProblemId)
            .Distinct()
            .CountAsync(ct);

        // 2. Language Preferences
        var langGroups = await _context.Submissions
            .Where(s => s.UserId == request.UserId)
            .GroupBy(s => s.Language)
            .Select(g => new { Language = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int totalSubmissions = langGroups.Sum(g => g.Count);
        var topLanguages = new List<LanguagePreferenceDto>();

        if (totalSubmissions > 0)
        {
            var colors = new Dictionary<string, string>
            {
                { "python", "#7c3aed" },
                { "javascript", "#10b981" },
                { "java", "#6366f1" },
                { "csharp", "#f59e0b" },
                { "cpp", "#ef4444" },
                { "c++", "#ef4444" },
                { "go", "#06b6d4" }
            };

            foreach (var lg in langGroups.OrderByDescending(g => g.Count).Take(4))
            {
                int pct = (int)Math.Round((double)lg.Count / totalSubmissions * 100);
                string color = colors.ContainsKey(lg.Language.ToLower()) ? colors[lg.Language.ToLower()] : "#8b949e";
                
                // Capitalize first letter or custom logic
                string langName = lg.Language;
                if (langName.Equals("csharp", StringComparison.OrdinalIgnoreCase)) langName = "C#";
                else if (langName.Equals("cpp", StringComparison.OrdinalIgnoreCase)) langName = "C++";
                else if (langName.Length > 0) langName = char.ToUpper(langName[0]) + langName.Substring(1).ToLower();

                topLanguages.Add(new LanguagePreferenceDto(langName, pct, color));
            }
        }

        // 3. Battle Records (Recent Activity)
        var battles = await _context.BattleRecords
            .Where(b => b.UserId == request.UserId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        // Seed some battles if none exist, just for demo
        if (!battles.Any())
        {
            var demoBattles = new List<BattleRecord>
            {
                BattleRecord.Create(request.UserId, "ByteWizard", "Two Sum", "Python", "12:29", 98, true, 18),
                BattleRecord.Create(request.UserId, "CodeNinja", "LRU Cache", "JavaScript", "18:44", 92, true, 22),
                BattleRecord.Create(request.UserId, "AlgoMaster", "Merge K Sorted Lists", "Python", "20:00", 61, false, -15)
            };
            
            _context.BattleRecords.AddRange(demoBattles);
            await _context.SaveChangesAsync(ct);
            battles = demoBattles;
        }

        var matchHistory = battles.Select(b => new BattleRecordDto(
            b.OpponentName,
            b.ProblemName,
            b.IsWin ? "Win" : "Loss",
            b.Score,
            b.Language,
            b.Duration,
            GetRelativeTime(b.CreatedAt),
            b.EloChange
        )).ToList();

        int totalBattles = battles.Count;
        int wins = battles.Count(b => b.IsWin);
        string winRate = totalBattles > 0 ? $"{Math.Round((double)wins / totalBattles * 100)}%" : "0%";

        var dto = new ProfileStatsDto(
            TotalBattles: totalBattles,
            Wins: wins,
            WinRate: winRate,
            ProblemsSolved: problemsSolved,
            BestStreak: "3 days", // Hardcoded for now
            TopLanguages: topLanguages,
            MatchHistory: matchHistory
        );

        return Result<ProfileStatsDto>.Success(dto, "Profile stats retrieved");
    }

    private string GetRelativeTime(DateTime dateTime)
    {
        var ts = DateTime.UtcNow - dateTime;
        if (ts.TotalMinutes < 1) return "just now";
        if (ts.TotalHours < 1) return $"{(int)ts.TotalMinutes} mins ago";
        if (ts.TotalDays < 1) return $"{(int)ts.TotalHours} hours ago";
        if (ts.TotalDays < 2) return "1 day ago";
        return $"{(int)ts.TotalDays} days ago";
    }
}
