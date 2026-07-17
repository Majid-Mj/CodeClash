using System;
using System.Threading.Tasks;

namespace CodeClash.Application.Common.Interfaces;

public interface ITournamentNotificationService
{
    Task NotifyMatchStartedAsync(Guid tournamentId, Guid matchId, Guid? p1Id, Guid? p2Id, Guid battleId, Guid problemId, string? language);
    Task NotifyBracketUpdatedAsync(Guid tournamentId);
    Task NotifyTournamentCompletedAsync(Guid tournamentId, Guid winnerId, string winnerUsername);
    Task NotifyTournamentCreatedAsync(Guid tournamentId, string title, int? minRating, int? maxRating);
    Task NotifyTournamentRegistrationChangedAsync(Guid tournamentId, int participantCount);
    Task NotifyMatchCompletedAsync(Guid tournamentId, Guid matchId, Guid winnerId);
}

