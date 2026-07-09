using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.GenerateBracket;

public record GenerateBracketCommand(Guid TournamentId) : IRequest<IEnumerable<MatchDto>>;
