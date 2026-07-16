using System;
using System.Linq;
using System.Threading.Tasks;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.API.Services;

/// <summary>
/// Centralises tournament match history, ELO updates, and placement rewards so that
/// BattleResolutionService, TournamentBackgroundService (timeout / forfeit) and
/// SubmitMatchResultCommandHandler all write identical records.
///
/// Design decisions
/// ────────────────
/// • Per-match ELO  : same global ELO engine, but K=16 (half the 1v1 K=32) so bracket
///   luck doesn't swing ratings as hard as a direct 1v1 challenge.
/// • Match history  : one BattleRecord per participant, tagged with the tournament language
///   (or "Tournament") so the profile history page shows it naturally.
/// • Placement ELO  : paid out ONCE when the tournament flips to Completed.
///   Bonus deltas: 1st +50, 2nd +25, 3rd/4th (semi) +10, quarter +5.
///   This is additive on top of the per-match deltas already earned during the bracket.
/// • Points         : TournamentResult.TotalPoints (100/50/25/10/5) are credited via
///   AddPoints when the tournament completes — that path already exists in all callers.
///   Placement ELO bonus is added alongside it here via ApplyPlacementRewardsAsync.
/// </summary>
public class TournamentMatchRewardService : ITournamentMatchRewardService
{
    private readonly IApplicationDbContext _context;

    // Tournament matches use a dampened K factor so bracket luck hurts less than 1v1.
    private const int TournamentKFactor = 16;

    public TournamentMatchRewardService(IApplicationDbContext context)
    {
        _context = context;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Per-match: write history + apply ELO delta
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Records a BattleRecord for both players and applies a dampened ELO delta.
    /// Call this AFTER the atomic SQL update that marks the match Completed.
    /// </summary>
    public async Task ApplyMatchRewardsAsync(
        Guid matchId,
        Guid winnerId,
        Guid tournamentId,
        string? tournamentLanguage)
    {
        var match = await _context.TournamentMatches.FindAsync(new object[] { matchId });
        if (match == null) return;

        var loserId = match.Player1Id == winnerId ? match.Player2Id : match.Player1Id;
        if (loserId == null) return; // bye – no opponent

        var winnerUser = await _context.Users.FindAsync(new object[] { winnerId });
        var loserUser  = await _context.Users.FindAsync(new object[] { loserId.Value });
        if (winnerUser == null || loserUser == null) return;

        // ── ELO calculation (K=16) ──────────────────────────────────────────
        double expectedWinner = 1.0 / (1.0 + Math.Pow(10.0, (loserUser.Rating - winnerUser.Rating) / 400.0));
        double expectedLoser  = 1.0 / (1.0 + Math.Pow(10.0, (winnerUser.Rating - loserUser.Rating) / 400.0));

        int winnerDelta = Math.Max(1,  (int)Math.Round(TournamentKFactor * (1.0 - expectedWinner)));
        int loserDelta  = Math.Min(-1, (int)Math.Round(TournamentKFactor * (0.0 - expectedLoser)));

        winnerUser.UpdateRating(winnerDelta);
        loserUser.UpdateRating(loserDelta);

        // ── Problem/context label ────────────────────────────────────────────
        string? problemName = null;
        if (match.AssignedProblemId.HasValue)
        {
            var problem = await _context.Problems.FindAsync(new object[] { match.AssignedProblemId.Value });
            problemName = problem?.Title;
        }
        var label    = problemName ?? "Tournament Match";
        var lang     = tournamentLanguage ?? "Tournament";
        var roundStr = match.Round.ToString(); // "QuarterFinal" | "SemiFinal" | "Final"

        // ── BattleRecords (history) ──────────────────────────────────────────
        var winnerRecord = BattleRecord.Create(
            userId: winnerId,
            opponentName: loserUser.Username,
            problemName: $"[{roundStr}] {label}",
            language: lang,
            duration: match.StartTime.HasValue && match.EndTime.HasValue
                ? FormatDuration(match.EndTime.Value - match.StartTime.Value)
                : "N/A",
            score: 100,
            isWin: true,
            eloChange: winnerDelta);

        var loserRecord = BattleRecord.Create(
            userId: loserId.Value,
            opponentName: winnerUser.Username,
            problemName: $"[{roundStr}] {label}",
            language: lang,
            duration: match.StartTime.HasValue && match.EndTime.HasValue
                ? FormatDuration(match.EndTime.Value - match.StartTime.Value)
                : "N/A",
            score: 20,
            isWin: false,
            eloChange: loserDelta);

        _context.BattleRecords.Add(winnerRecord);
        _context.BattleRecords.Add(loserRecord);

        // ── In-match XP (small bonus for winning) ───────────────────────────
        winnerUser.AddPoints(5); // per-match winner bonus (smaller than 1v1)
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tournament completion: placement ELO bonuses
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called once when tournament.Status flips to Completed.
    /// Applies placement-based ELO bonuses on top of per-match deltas already earned.
    /// Points are credited separately by the existing Results loop in callers.
    /// </summary>
    public async Task ApplyPlacementRewardsAsync(Guid tournamentId)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.Results)
            .Include(t => t.Matches)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament == null) return;

        foreach (var result in tournament.Results)
        {
            var user = await _context.Users.FindAsync(new object[] { result.UserId });
            if (user == null) continue;

            int eloBonus = result.Rank switch
            {
                1 => 50,   // Champion
                2 => 25,   // Runner-up
                3 => 10,   // Semi-finalist
                4 => 5,    // Quarter-finalist
                _ => 0
            };

            if (eloBonus > 0)
                user.UpdateRating(eloBonus);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    private static string FormatDuration(TimeSpan ts) =>
        $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
}
