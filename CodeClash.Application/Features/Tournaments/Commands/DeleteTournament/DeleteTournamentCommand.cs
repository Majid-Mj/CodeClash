using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.DeleteTournament;

public record DeleteTournamentCommand(Guid Id) : IRequest;
