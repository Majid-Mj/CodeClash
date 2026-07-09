using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.CancelTournament;

public record CancelTournamentCommand(Guid Id) : IRequest;
