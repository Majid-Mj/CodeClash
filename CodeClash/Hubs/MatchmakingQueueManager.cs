using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CodeClash.Domain.Enums;

namespace CodeClash.API.Hubs;

public record QueueTicket(
    string ConnectionId,
    Guid UserId,
    string Username,
    int Rating,
    string PreferredLanguage,
    Difficulty Difficulty,
    DateTime QueuedAt
);

public interface IMatchmakingQueueManager
{
    void Enqueue(QueueTicket ticket);
    void Dequeue(string connectionId);
    QueueTicket? GetTicket(string connectionId);
    List<QueueTicket> GetAllTickets();
    List<(QueueTicket Player1, QueueTicket Player2)> FindMatches();
}

public class MatchmakingQueueManager : IMatchmakingQueueManager
{
    private readonly ConcurrentDictionary<string, QueueTicket> _queue = new();

    public void Enqueue(QueueTicket ticket)
    {
        _queue[ticket.ConnectionId] = ticket;
    }

    public void Dequeue(string connectionId)
    {
        _queue.TryRemove(connectionId, out _);
    }

    public QueueTicket? GetTicket(string connectionId)
    {
        return _queue.TryGetValue(connectionId, out var ticket) ? ticket : null;
    }

    public List<QueueTicket> GetAllTickets() => _queue.Values.ToList();

    public List<(QueueTicket Player1, QueueTicket Player2)> FindMatches()
    {
        var matchedPairs = new List<(QueueTicket, QueueTicket)>();
        var tickets = _queue.Values.OrderBy(t => t.QueuedAt).ToList();
        var processed = new HashSet<string>();

        for (int i = 0; i < tickets.Count; i++)
        {
            var t1 = tickets[i];
            if (processed.Contains(t1.ConnectionId)) continue;

            for (int j = i + 1; j < tickets.Count; j++)
            {
                var t2 = tickets[j];
                if (processed.Contains(t2.ConnectionId)) continue;

                // Match criteria: Same difficulty rating and ELO difference is within tolerance.
                // Rating tolerance dynamically grows over queue wait time (+50 ELO limit increment every 5 seconds).
                if (t1.UserId != t2.UserId && t1.Difficulty == t2.Difficulty && t1.PreferredLanguage.Equals(t2.PreferredLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    double waitTimeSeconds = Math.Min(
                        (DateTime.UtcNow - t1.QueuedAt).TotalSeconds,
                        (DateTime.UtcNow - t2.QueuedAt).TotalSeconds
                    );
                    int maxEloTolerance = 100 + (int)(waitTimeSeconds / 5.0) * 50;

                    if (Math.Abs(t1.Rating - t2.Rating) <= maxEloTolerance)
                    {
                        matchedPairs.Add((t1, t2));
                        processed.Add(t1.ConnectionId);
                        processed.Add(t2.ConnectionId);
                        _queue.TryRemove(t1.ConnectionId, out _);
                        _queue.TryRemove(t2.ConnectionId, out _);
                        break;
                    }
                }
            }
        }

        return matchedPairs;
    }
}
