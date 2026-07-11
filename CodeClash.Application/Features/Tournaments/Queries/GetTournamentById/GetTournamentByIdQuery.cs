using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentById;

public record GetTournamentByIdQuery(Guid Id) : IRequest<TournamentDto>;
