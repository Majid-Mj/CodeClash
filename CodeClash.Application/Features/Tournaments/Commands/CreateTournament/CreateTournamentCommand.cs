using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.CreateTournament;

public record CreateTournamentCommand(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    int MaxParticipants) : IRequest<Guid>;
