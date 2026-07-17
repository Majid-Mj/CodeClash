using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.ScheduleMatch;

public record ScheduleMatchCommand(Guid TournamentId, Guid MatchId, DateTime ScheduledTime, Guid UserId, bool IsAdmin) : IRequest;
