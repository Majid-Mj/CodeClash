using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.UpdateTournament;

public record UpdateTournamentCommand(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    int MaxParticipants,
    int? MinRating = null,
    int? MaxRating = null,
    string? Language = null) : IRequest;
