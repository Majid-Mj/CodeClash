using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.RegisterUser;

public record RegisterUserCommand(Guid TournamentId, Guid UserId) : IRequest;
