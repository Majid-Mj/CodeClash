using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Commands.ScheduleMatch;

public class ScheduleMatchCommandHandler : IRequestHandler<ScheduleMatchCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;
    private readonly ITournamentNotificationService _tournamentNotificationService;

    public ScheduleMatchCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context,
        ITournamentNotificationService tournamentNotificationService)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
        _tournamentNotificationService = tournamentNotificationService;
    }

    public async Task Handle(ScheduleMatchCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(request.TournamentId, cancellationToken);
        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.TournamentId} not found.");
        }

        var match = tournament.Matches.FirstOrDefault(m => m.Id == request.MatchId);
        if (match == null)
        {
            throw new KeyNotFoundException($"Match with Id {request.MatchId} not found.");
        }

        if (match.Status != MatchStatus.Scheduled)
        {
            throw new InvalidOperationException("Only scheduled matches can have their time modified.");
        }

        // Authorize: Admin or match participant
        if (!request.IsAdmin && match.Player1Id != request.UserId && match.Player2Id != request.UserId)
        {
            throw new UnauthorizedAccessException("Only the match participants or an admin can schedule this match.");
        }

        var scheduledTimeUtc = request.ScheduledTime.Kind == DateTimeKind.Utc 
            ? request.ScheduledTime 
            : DateTime.SpecifyKind(request.ScheduledTime, DateTimeKind.Utc);

        match.SetScheduledTime(scheduledTimeUtc);

        // Explicitly set entity state to Modified to guarantee EF Core detects and persists the change
        var dbContext = (DbContext)_context;
        dbContext.Entry(match).State = EntityState.Modified;

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _tournamentNotificationService.NotifyMatchScheduledAsync(tournament.Id, match.Id, match.Player1Id, match.Player2Id, scheduledTimeUtc);
    }
}
