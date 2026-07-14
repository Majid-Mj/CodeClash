using System;
using System.Threading;
using System.Threading.Tasks;
using CodeClash.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CodeClash.API.Hubs;

public class DuelNotificationService : IDuelNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public DuelNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyDuelStartedAsync(Guid roomId, string roomCode, Guid problemId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group(roomId.ToString()).SendAsync("DuelStarted", new
        {
            roomId,
            roomCode,
            problemId
        }, ct);
    }

    public async Task NotifyDuelEndedAsync(Guid roomId, Guid winnerId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group(roomId.ToString()).SendAsync("DuelEnded", new
        {
            roomId,
            winnerId,
            status = "Completed"
        }, ct);
    }

    public async Task NotifyPlayerLeftAsync(Guid roomId, Guid userId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group(roomId.ToString()).SendAsync("PlayerLeft", new
        {
            roomId,
            userId
        }, ct);
    }
}
