using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournaments;

public record GetTournamentsQuery : IRequest<IEnumerable<TournamentDto>>;
