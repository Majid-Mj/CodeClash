using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentParticipants;

public record GetTournamentParticipantsQuery(Guid TournamentId) : IRequest<IEnumerable<ParticipantDto>>;
