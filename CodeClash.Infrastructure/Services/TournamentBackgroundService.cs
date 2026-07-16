using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Tournaments.Commands.StartMatch;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeClash.Infrastructure.Services;

public class TournamentBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TournamentBackgroundService> _logger;
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MatchTimeoutLimit = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan JoinTimeoutLimit = TimeSpan.FromSeconds(90);

    public TournamentBackgroundService(IServiceProvider serviceProvider, ILogger<TournamentBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TournamentBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTournamentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tournaments in background service.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("TournamentBackgroundService stopped.");
    }

    private async Task ProcessTournamentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var notificationService = scope.ServiceProvider.GetRequiredService<ITournamentNotificationService>();
        var rewardService = scope.ServiceProvider.GetRequiredService<ITournamentMatchRewardService>();

        // 1. Get live tournaments
        var liveTournaments = await context.Tournaments
            .Include(t => t.Matches)
            .Include(t => t.Registrations)
            .Where(t => t.Status == TournamentStatus.Live)
            .ToListAsync(cancellationToken);

        foreach (var tournament in liveTournaments)
        {
            // 2. Start Scheduled Matches
            var upcomingMatches = tournament.Matches
                .Where(m => m.Status == MatchStatus.Scheduled 
                    && m.ScheduledTime.Year > 1970 
                    && m.ScheduledTime <= DateTime.UtcNow)
                .ToList();

            foreach (var match in upcomingMatches)
            {
                // Bye Handling: If Player1 exists but Player2 does not, player 1 wins automatically
                if (match.Player1Id.HasValue && !match.Player2Id.HasValue)
                {
                    _logger.LogInformation("Match {MatchId} has a bye. Player {WinnerId} wins.", match.Id, match.Player1Id.Value);
                    tournament.SubmitMatchResult(match.Id, match.Player1Id.Value);
                    await notificationService.NotifyBracketUpdatedAsync(tournament.Id);
                    if (tournament.Status == TournamentStatus.Completed)
                    {
                        var winnerUser = await context.Users.FindAsync(new object[] { match.Player1Id.Value });
                        var winnerUsername = winnerUser?.Username ?? "Champion";
                        await notificationService.NotifyTournamentCompletedAsync(tournament.Id, match.Player1Id.Value, winnerUsername);
                    }
                    continue;
                }
                
                if (!match.Player1Id.HasValue && match.Player2Id.HasValue)
                {
                    _logger.LogInformation("Match {MatchId} has a bye. Player {WinnerId} wins.", match.Id, match.Player2Id.Value);
                    tournament.SubmitMatchResult(match.Id, match.Player2Id.Value);
                    await notificationService.NotifyBracketUpdatedAsync(tournament.Id);
                    if (tournament.Status == TournamentStatus.Completed)
                    {
                        var winnerUser = await context.Users.FindAsync(new object[] { match.Player2Id.Value });
                        var winnerUsername = winnerUser?.Username ?? "Champion";
                        await notificationService.NotifyTournamentCompletedAsync(tournament.Id, match.Player2Id.Value, winnerUsername);
                    }
                    continue;
                }

                // If both players are present, start the match!
                if (match.Player1Id.HasValue && match.Player2Id.HasValue)
                {
                    try
                    {
                        _logger.LogInformation("Starting match {MatchId} for tournament {TournamentId}.", match.Id, tournament.Id);
                        await mediator.Send(new StartMatchCommand(tournament.Id, match.Id), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to start match {MatchId} automatically.", match.Id);
                    }
                }
            }

            // 3. Resolve Timed Out / Stuck Matches
            var liveMatches = tournament.Matches
                .Where(m => m.Status == MatchStatus.InProgress && m.StartTime.HasValue && (DateTime.UtcNow - m.StartTime.Value) > MatchTimeoutLimit)
                .ToList();

            foreach (var match in liveMatches)
            {
                _logger.LogWarning("Match {MatchId} has timed out. Forcing resolution.", match.Id);
                
                // Get submissions for both players during this match to evaluate who solved more test cases
                var p1Submissions = await context.Submissions
                    .Where(s => s.ProblemId == match.AssignedProblemId && s.UserId == match.Player1Id && s.CreatedAt >= match.StartTime)
                    .ToListAsync(cancellationToken);

                var p2Submissions = await context.Submissions
                    .Where(s => s.ProblemId == match.AssignedProblemId && s.UserId == match.Player2Id && s.CreatedAt >= match.StartTime)
                    .ToListAsync(cancellationToken);

                var p1MaxPassed = p1Submissions.Any() ? p1Submissions.Max(s => s.PassedCount) : 0;
                var p2MaxPassed = p2Submissions.Any() ? p2Submissions.Max(s => s.PassedCount) : 0;

                Guid winnerId = match.Player1Id ?? Guid.Empty;

                if (p1MaxPassed > p2MaxPassed)
                {
                    winnerId = match.Player1Id!.Value;
                }
                else if (p2MaxPassed > p1MaxPassed)
                {
                    winnerId = match.Player2Id!.Value;
                }
                else if (match.Player1Id.HasValue)
                {
                    // Fallback: If both have equal test cases, pick player 1
                    winnerId = match.Player1Id.Value;
                }

                if (winnerId != Guid.Empty)
                {
                    _logger.LogInformation("Timeout Winner for Match {MatchId} determined as Player {WinnerId}.", match.Id, winnerId);
                    
                    var dbContext = context as DbContext;
                    int affected = 0;
                    if (dbContext != null)
                    {
                        affected = await dbContext.Database.ExecuteSqlRawAsync(
                            "UPDATE TournamentMatches SET Status = 'Completed', WinnerId = {0}, EndTime = {1} WHERE Id = {2} AND (Status = 'InProgress' OR Status = 'Live')",
                            new object[] { winnerId, DateTime.UtcNow, match.Id },
                            cancellationToken);
                    }

                    if (affected > 0)
                    {
                        // Write history + dampened ELO (K=16)
                        await rewardService.ApplyMatchRewardsAsync(match.Id, winnerId, tournament.Id, tournament.Language);

                        tournament.SubmitMatchResult(match.Id, winnerId);
                        await notificationService.NotifyBracketUpdatedAsync(tournament.Id);
                        await notificationService.NotifyMatchCompletedAsync(tournament.Id, match.Id, winnerId);
                        if (tournament.Status == TournamentStatus.Completed)
                        {
                            // Placement XP
                            foreach (var pr in tournament.Results)
                            {
                                var u = await context.Users.FindAsync(new object[] { pr.UserId });
                                if (u != null) u.AddPoints(pr.TotalPoints);
                            }
                            // Placement ELO bonuses
                            await rewardService.ApplyPlacementRewardsAsync(tournament.Id);

                            var winnerUser = await context.Users.FindAsync(new object[] { winnerId });
                            var winnerUsername = winnerUser?.Username ?? "Champion";
                            await notificationService.NotifyTournamentCompletedAsync(tournament.Id, winnerId, winnerUsername);
                        }
                    }
                }
            }

            // 4. Auto-forfeit players who never joined their battle room within the join timeout window
            var joinCheckMatches = tournament.Matches
                .Where(m => m.Status == MatchStatus.InProgress
                    && m.StartTime.HasValue
                    && m.BattleId.HasValue
                    && (DateTime.UtcNow - m.StartTime.Value) >= JoinTimeoutLimit
                    && (DateTime.UtcNow - m.StartTime.Value) < MatchTimeoutLimit) // avoid double-processing matches already handled by Step 3
                .ToList();

            foreach (var match in joinCheckMatches)
            {
                var participants = await context.BattleParticipants
                    .Where(bp => bp.BattleId == match.BattleId!.Value)
                    .ToListAsync(cancellationToken);

                var p1Joined = participants.Any(bp => bp.UserId == match.Player1Id && bp.HasJoinedRoom);
                var p2Joined = participants.Any(bp => bp.UserId == match.Player2Id && bp.HasJoinedRoom);

                // Only act if at least one player is absent
                if (p1Joined && p2Joined) continue;

                Guid? winnerId = null;

                if (p1Joined && !p2Joined)
                {
                    winnerId = match.Player1Id;
                    _logger.LogWarning("Match {MatchId}: Player2 {P2Id} never joined the battle room. Forfeiting in favour of Player1.", match.Id, match.Player2Id);
                }
                else if (!p1Joined && p2Joined)
                {
                    winnerId = match.Player2Id;
                    _logger.LogWarning("Match {MatchId}: Player1 {P1Id} never joined the battle room. Forfeiting in favour of Player2.", match.Id, match.Player1Id);
                }
                else
                {
                    // Both absent – default to Player1 so the bracket doesn't stall
                    winnerId = match.Player1Id;
                    _logger.LogWarning("Match {MatchId}: Neither player joined the battle room within {Timeout}s. Defaulting winner to Player1.", match.Id, JoinTimeoutLimit.TotalSeconds);
                }

                if (!winnerId.HasValue) continue;

                var dbContext = context as DbContext;
                int affected = 0;
                if (dbContext != null)
                {
                    affected = await dbContext.Database.ExecuteSqlRawAsync(
                        "UPDATE TournamentMatches SET Status = 'Completed', WinnerId = {0}, EndTime = {1} WHERE Id = {2} AND (Status = 'InProgress' OR Status = 'Live')",
                        new object[] { winnerId.Value, DateTime.UtcNow, match.Id },
                        cancellationToken);
                }

                if (affected > 0)
                {
                    // Mark the underlying Battle as completed
                    var battle = await context.Battles.FindAsync(new object[] { match.BattleId!.Value });
                    if (battle != null && battle.Status == BattleStatus.InProgress)
                        battle.Complete(winnerId.Value);

                    // Send forfeit notifications to both players
                    if (match.Player1Id.HasValue)
                        context.Notifications.Add(new Notification(match.Player1Id.Value, "Match Forfeited", "A tournament match was forfeited due to a player not joining in time.", "warning"));
                    if (match.Player2Id.HasValue)
                        context.Notifications.Add(new Notification(match.Player2Id.Value, "Match Forfeited", "A tournament match was forfeited due to a player not joining in time.", "warning"));

                    // Write history + dampened ELO (K=16)
                    await rewardService.ApplyMatchRewardsAsync(match.Id, winnerId.Value, tournament.Id, tournament.Language);

                    tournament.SubmitMatchResult(match.Id, winnerId.Value);
                    await notificationService.NotifyBracketUpdatedAsync(tournament.Id);
                    await notificationService.NotifyMatchCompletedAsync(tournament.Id, match.Id, winnerId.Value);
                    if (tournament.Status == TournamentStatus.Completed)
                    {
                        // Placement XP
                        foreach (var pr in tournament.Results)
                        {
                            var u = await context.Users.FindAsync(new object[] { pr.UserId });
                            if (u != null) u.AddPoints(pr.TotalPoints);
                        }
                        // Placement ELO bonuses
                        await rewardService.ApplyPlacementRewardsAsync(tournament.Id);

                        var winnerUser = await context.Users.FindAsync(new object[] { winnerId.Value });
                        var winnerUsername = winnerUser?.Username ?? "Champion";
                        await notificationService.NotifyTournamentCompletedAsync(tournament.Id, winnerId.Value, winnerUsername);
                    }
                }
            }
        }

        // Save changes across all tournaments processed in this tick
        await context.SaveChangesAsync(cancellationToken);
    }
}
