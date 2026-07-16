using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.CreateTournament;

public record CreateTournamentCommand(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    int MaxParticipants,
    int? MinRating = null,
    int? MaxRating = null,
    string? Language = null) : IRequest<Guid>;
