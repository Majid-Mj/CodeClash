using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Commands.SubmitMatchResult;

public class SubmitMatchResultCommandHandler : IRequestHandler<SubmitMatchResultCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;
    private readonly ITournamentNotificationService _notificationService;
    private readonly ITournamentMatchRewardService _rewardService;

    public SubmitMatchResultCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context,
        ITournamentNotificationService notificationService,
        ITournamentMatchRewardService rewardService)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
        _notificationService = notificationService;
        _rewardService = rewardService;
    }

    public async Task Handle(SubmitMatchResultCommand request, CancellationToken cancellationToken)
    {
        var dbContext = _context as DbContext;
        using var transaction = dbContext != null ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        try
        {
            var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(request.TournamentId, cancellationToken);
            if (tournament == null)
                throw new KeyNotFoundException($"Tournament with Id {request.TournamentId} not found.");

            var match = tournament.Matches.FirstOrDefault(m => m.Id == request.MatchId);
            if (match == null || match.Status != MatchStatus.InProgress)
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                return;
            }

            // Atomic guard — prevents double-writes from concurrent requests
            var affected = await _tournamentRepository.ExecuteAtomicMatchResultUpdateAsync(request.MatchId, request.WinnerId, cancellationToken);

            if (affected == 0)
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                return;
            }

            // Write per-match BattleRecord history + apply dampened ELO (K=16)
            await _rewardService.ApplyMatchRewardsAsync(
                request.MatchId, request.WinnerId, request.TournamentId, tournament.Language);

            // Sync domain model so SaveChangesAsync writes next-round slot + tournament status
            tournament.SubmitMatchResult(request.MatchId, request.WinnerId);

            // Placement rewards paid out once when tournament flips to Completed
            if (tournament.Status == Domain.Enums.TournamentStatus.Completed)
            {
                foreach (var pr in tournament.Results)
                {
                    var u = await _context.Users.FindAsync(new object[] { pr.UserId }, cancellationToken);
                    if (u != null) u.AddPoints(pr.TotalPoints);
                }
                await _rewardService.ApplyPlacementRewardsAsync(request.TournamentId);
            }

            await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null) await transaction.CommitAsync(cancellationToken);

            // SignalR notifications (outside transaction — best-effort)
            await _notificationService.NotifyBracketUpdatedAsync(request.TournamentId);
            await _notificationService.NotifyMatchCompletedAsync(request.TournamentId, request.MatchId, request.WinnerId);

            if (tournament.Status == Domain.Enums.TournamentStatus.Completed)
            {
                var winnerUser = await _context.Users.FindAsync(new object[] { request.WinnerId }, cancellationToken);
                await _notificationService.NotifyTournamentCompletedAsync(
                    request.TournamentId, request.WinnerId, winnerUser?.Username ?? "Champion");
            }
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}


