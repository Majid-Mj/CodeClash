using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentDetails;

public record GetTournamentDetailsQuery(Guid Id) : IRequest<TournamentDetailsDto>;
