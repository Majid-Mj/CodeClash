using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentMatches;

public record GetTournamentMatchesQuery(Guid TournamentId) : IRequest<IEnumerable<MatchDto>>;
