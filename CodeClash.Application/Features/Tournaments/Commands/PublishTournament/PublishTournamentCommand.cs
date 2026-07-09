using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.PublishTournament;

public record PublishTournamentCommand(Guid Id) : IRequest;
