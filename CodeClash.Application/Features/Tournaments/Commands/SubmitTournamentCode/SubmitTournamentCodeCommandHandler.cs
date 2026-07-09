using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Submissions.Commands.RunCode;
using CodeClash.Application.Features.Submissions.DTOs;
using CodeClash.Application.Features.Tournaments.Commands.SubmitMatchResult;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Commands.SubmitTournamentCode;

public class SubmitTournamentCodeCommandHandler : IRequestHandler<SubmitTournamentCodeCommand, SubmissionResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public SubmitTournamentCodeCommandHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<SubmissionResponseDto> Handle(SubmitTournamentCodeCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Match
        var match = await _context.TournamentMatches
            .FirstOrDefaultAsync(m => m.Id == request.MatchId && m.TournamentId == request.TournamentId, cancellationToken);
            
        if (match == null)
            throw new KeyNotFoundException("Match not found.");

        if (match.Status != MatchStatus.Live)
            throw new InvalidOperationException("Match is not currently live.");

        if (match.Player1Id != request.UserId && match.Player2Id != request.UserId)
            throw new UnauthorizedAccessException("You are not a participant in this match.");

        if (!match.AssignedProblemId.HasValue)
            throw new InvalidOperationException("No problem assigned to this match.");

        // 2. Delegate to RunCodeCommand
        var runDto = new CreateSubmissionRequestDto(match.AssignedProblemId.Value, request.Language, request.SourceCode);
        var runCommand = new RunCodeCommand(runDto, request.UserId);
        
        var runResult = await _mediator.Send(runCommand, cancellationToken);
        if (!runResult.IsSuccess || runResult.Data == null)
        {
            throw new Exception("Code execution failed: " + runResult.Error);
        }

        var submissionResponse = runResult.Data;

        // 3. Check for Win condition
        if (submissionResponse.Status == SubmissionStatus.Accepted.ToString())
        {
            // Trigger SubmitMatchResultCommand to advance winner
            await _mediator.Send(new SubmitMatchResultCommand(request.TournamentId, request.MatchId, request.UserId), cancellationToken);
        }

        return submissionResponse;
    }
}
