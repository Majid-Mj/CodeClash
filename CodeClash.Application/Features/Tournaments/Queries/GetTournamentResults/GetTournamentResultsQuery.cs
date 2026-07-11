using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentResults;

public record GetTournamentResultsQuery(Guid TournamentId) : IRequest<IEnumerable<TournamentResultDto>>;
