using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.SubmitMatchResult;

public record SubmitMatchResultCommand(Guid TournamentId, Guid MatchId, Guid WinnerId) : IRequest;
