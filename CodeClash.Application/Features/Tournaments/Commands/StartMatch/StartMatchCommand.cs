using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.StartMatch;

public record StartMatchCommand(Guid TournamentId, Guid MatchId) : IRequest;
