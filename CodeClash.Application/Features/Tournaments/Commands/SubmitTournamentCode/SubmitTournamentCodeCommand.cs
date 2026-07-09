using CodeClash.Application.Features.Submissions.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.SubmitTournamentCode;

public record SubmitTournamentCodeCommand(
    Guid TournamentId, 
    Guid MatchId, 
    Guid UserId, 
    string Language, 
    string SourceCode) : IRequest<SubmissionResponseDto>;
