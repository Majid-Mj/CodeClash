using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.UnregisterUser;

public record UnregisterUserCommand(Guid TournamentId, Guid UserId) : IRequest;
