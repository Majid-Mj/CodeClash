using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CodeClash.Application.Common.Interfaces;
using CodeClash.API.Hubs;
using CodeClash.Domain.Entities;

namespace CodeClash.API.Services;

public class TournamentNotificationService : ITournamentNotificationService
{
    private readonly IHubContext<TournamentHub> _hubContext;
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly IApplicationDbContext _dbContext;

    public TournamentNotificationService(
        IHubContext<TournamentHub> hubContext,
        IHubContext<NotificationHub> notificationHubContext,
        IApplicationDbContext dbContext)
    {
        _hubContext = hubContext;
        _notificationHubContext = notificationHubContext;
        _dbContext = dbContext;
    }

    public async Task NotifyMatchStartedAsync(Guid tournamentId, Guid matchId, Guid? p1Id, Guid? p2Id, Guid battleId, string? language)
    {
        await _hubContext.Clients.Group($"Tournament_{tournamentId}").SendAsync("MatchStarted", new
        {
            tournamentId,
            matchId,
            player1Id = p1Id,
            player2Id = p2Id,
            battleId,
            language
        });
    }

    public async Task NotifyBracketUpdatedAsync(Guid tournamentId)
    {
        await _hubContext.Clients.Group($"Tournament_{tournamentId}").SendAsync("BracketUpdated", new
        {
            tournamentId
        });
    }

    public async Task NotifyTournamentCompletedAsync(Guid tournamentId, Guid winnerId, string winnerUsername)
    {
        await _hubContext.Clients.Group($"Tournament_{tournamentId}").SendAsync("TournamentCompleted", new
        {
            tournamentId,
            winnerId,
            winnerUsername
        });
    }

    public async Task NotifyTournamentCreatedAsync(Guid tournamentId, string title, int? minRating, int? maxRating)
    {
        var query = _dbContext.Users.Where(u => u.IsActive && u.Role != CodeClash.Domain.Enums.UserRole.Admin);

        if (minRating.HasValue)
        {
            query = query.Where(u => u.Rating >= minRating.Value);
        }

        if (maxRating.HasValue)
        {
            query = query.Where(u => u.Rating <= maxRating.Value);
        }

        var eligibleUsers = await query.ToListAsync();

        var message = $"A new tournament '{title}' has been created! ";
        if (minRating.HasValue && maxRating.HasValue)
        {
            message += $"Eligibility rating: {minRating.Value} - {maxRating.Value}.";
        }
        else if (minRating.HasValue)
        {
            message += $"Min eligibility rating: {minRating.Value}.";
        }
        else if (maxRating.HasValue)
        {
            message += $"Max eligibility rating: {maxRating.Value}.";
        }
        else
        {
            message += "Open to all ratings.";
        }

        // Save persistent notifications to DB for each eligible user
        var notifications = eligibleUsers.Select(u => new Notification(u.Id, "New Tournament Created", message, "info")).ToList();
        _dbContext.Notifications.AddRange(notifications);
        await _dbContext.SaveChangesAsync(default);

        // Push targeted real-time notifications via NotificationHub to each user
        foreach (var user in eligibleUsers)
        {
            await _notificationHubContext.Clients.User(user.Id.ToString()).SendAsync("ReceiveNotification", new
            {
                title = "New Tournament Created",
                message = message,
                type = "info"
            });
        }

        // Broadcast to all on TournamentHub
        await _hubContext.Clients.All.SendAsync("TournamentCreated", new
        {
            tournamentId,
            title,
            minRating,
            maxRating
        });
    }

    public async Task NotifyTournamentRegistrationChangedAsync(Guid tournamentId, int participantCount)
    {
        await _hubContext.Clients.Group("AdminDashboard").SendAsync("TournamentRegistrantIncremented", new
        {
            tournamentId,
            participantCount
        });
    }

    public async Task NotifyMatchCompletedAsync(Guid tournamentId, Guid matchId, Guid winnerId)
    {
        await _hubContext.Clients.Group($"Tournament_{tournamentId}").SendAsync("MatchCompleted", new
        {
            tournamentId,
            matchId,
            winnerId
        });

        await _hubContext.Clients.Group("AdminDashboard").SendAsync("MatchCompleted", new
        {
            tournamentId,
            matchId,
            winnerId
        });
    }
}

