using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Common.Interfaces;

public interface IDuelNotificationService
{
    Task NotifyDuelStartedAsync(Guid roomId, string roomCode, Guid problemId, CancellationToken ct = default);
    Task NotifyDuelEndedAsync(Guid roomId, Guid winnerId, CancellationToken ct = default);
    Task NotifyPlayerLeftAsync(Guid roomId, Guid userId, CancellationToken ct = default);
}
