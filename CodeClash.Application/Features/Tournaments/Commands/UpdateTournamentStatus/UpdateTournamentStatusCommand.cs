using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.UpdateTournamentStatus;

public record UpdateTournamentStatusCommand(Guid Id, string Status) : IRequest<Unit>;
