using System;
using System.Threading.Tasks;

namespace CodeClash.Application.Common.Interfaces;

public interface ITournamentMatchRewardService
{
    /// <summary>
    /// Writes BattleRecord history for both players and applies a dampened (K=16)
    /// ELO delta to each user. Call this after the atomic match-complete SQL guard succeeds.
    /// </summary>
    Task ApplyMatchRewardsAsync(
        Guid matchId,
        Guid winnerId,
        Guid tournamentId,
        string? tournamentLanguage);

    /// <summary>
    /// Applies placement-based ELO bonuses (50/25/10/5) once the tournament flips to
    /// Completed. Must be called exactly once — guarded by tournament.Status check in callers.
    /// </summary>
    Task ApplyPlacementRewardsAsync(Guid tournamentId);
}
