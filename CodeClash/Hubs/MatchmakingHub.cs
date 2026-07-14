using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CodeClash.API.Hubs;

/// <summary>Maps frontend display names to DB-stored language identifiers.</summary>
file static class LanguageNormalizer
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        { "c#",         "csharp"     },
        { "csharp",     "csharp"     },
        { "c++",        "cpp"        },
        { "cpp",        "cpp"        },
        { "javascript", "javascript" },
        { "js",         "javascript" },
        { "typescript", "typescript" },
        { "ts",         "typescript" },
        { "python",     "python"     },
        { "py",         "python"     },
        { "java",       "java"       },
        { "go",         "go"         },
        { "golang",     "go"         },
        { "rust",       "rust"       },
    };

    public static string Normalize(string lang)
        => _map.TryGetValue(lang.Trim(), out var normalized) ? normalized : lang.Trim().ToLowerInvariant();
}

[Authorize]
public class MatchmakingHub : Hub
{
    private readonly IMatchmakingQueueManager _queueManager;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<MatchmakingHub> _logger;

    public MatchmakingHub(IMatchmakingQueueManager queueManager, IApplicationDbContext context, ILogger<MatchmakingHub> logger)
    {
        _queueManager = queueManager;
        _context = context;
        _logger = logger;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("[MM] User disconnected. ConnectionId={ConnectionId}", Context.ConnectionId);
        _queueManager.Dequeue(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task JoinQueue(string preferredLanguage, string difficultyStr)
    {
        var connectionId = Context.ConnectionId;
        var userIdStr = Context.UserIdentifier;

        _logger.LogInformation("[MM] JoinQueue called. UserId={UserId}, Lang={Lang}, Diff={Diff}",
            userIdStr, preferredLanguage, difficultyStr);

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("[MM] Unauthorized JoinQueue attempt.");
            throw new HubException("Unauthorized connection.");
        }

        var user = await _context.Users.FindAsync(new object[] { userId });
        if (user == null)
        {
            _logger.LogWarning("[MM] User not found: {UserId}", userId);
            throw new HubException("User not found.");
        }

        if (!Enum.TryParse<Difficulty>(difficultyStr, true, out var difficulty))
        {
            difficulty = Difficulty.Medium;
        }

        var ticket = new QueueTicket(
            connectionId,
            userId,
            user.Username,
            user.Rating,
            preferredLanguage,
            difficulty,
            DateTime.UtcNow
        );

        _queueManager.Enqueue(ticket);

        var allTickets = _queueManager.GetAllTickets();
        _logger.LogInformation("[MM] Queue after enqueue: {Count} tickets total. Players: {Players}",
            allTickets.Count,
            string.Join(", ", allTickets.Select(t => $"{t.Username}({t.PreferredLanguage}/{t.Difficulty})")));

        // Run matchmaker loop
        await RunMatchmakingPassAsync();
    }

    public Task LeaveQueue()
    {
        _logger.LogInformation("[MM] LeaveQueue called. ConnectionId={ConnectionId}", Context.ConnectionId);
        _queueManager.Dequeue(Context.ConnectionId);
        return Clients.Caller.SendAsync("QueueLeft");
    }

    private async Task RunMatchmakingPassAsync()
    {
        var allTickets = _queueManager.GetAllTickets();
        _logger.LogInformation("[MM] Running match pass. Queue size: {Count}", allTickets.Count);

        var matches = _queueManager.FindMatches();
        _logger.LogInformation("[MM] FindMatches returned {Count} pairs.", matches.Count);

        foreach (var (p1, p2) in matches)
        {
            _logger.LogInformation("[MM] Pairing {P1} vs {P2} (Lang={Lang}, Diff={Diff})",
                p1.Username, p2.Username, p1.PreferredLanguage, p1.Difficulty);

            // 1. Get all active, non-deleted problems of target difficulty
            var candidateProblems = await _context.Problems
                .Where(p => p.Difficulty == p1.Difficulty && p.DeletedAt == null && p.IsActive)
                .ToListAsync();

            _logger.LogInformation("[MM] Found {Count} candidate problems for difficulty {Diff}",
                candidateProblems.Count, p1.Difficulty);

            // Filter in-memory to match allowed language
            var targetLang = LanguageNormalizer.Normalize(p1.PreferredLanguage);
            _logger.LogInformation("[MM] Normalized language '{Raw}' → '{Normalized}'", p1.PreferredLanguage, targetLang);
            var matchingProblems = candidateProblems
                .Where(p => {
                    try
                    {
                        var allowed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.AllowedLanguagesJson);
                        return allowed != null && allowed.Any(l => l.Equals(targetLang, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return false;
                    }
                })
                .ToList();

            _logger.LogInformation("[MM] {Count} problems match lang={Lang}", matchingProblems.Count, targetLang);

            Problem? problem = null;

            if (matchingProblems.Any())
            {
                var rand = new Random();
                problem = matchingProblems[rand.Next(matchingProblems.Count)];
                _logger.LogInformation("[MM] Selected problem: {Title}", problem.Title);
            }
            else
            {
                _logger.LogWarning("[MM] No exact match for lang={Lang}+diff={Diff}. Trying fallback.", targetLang, p1.Difficulty);

                // Fallback: search for ANY difficulty problem supporting this language
                var fallbackCandidates = await _context.Problems
                    .Where(p => p.DeletedAt == null && p.IsActive)
                    .ToListAsync();

                var fallbackMatches = fallbackCandidates
                    .Where(p => {
                        try
                        {
                            var allowed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.AllowedLanguagesJson);
                            return allowed != null && allowed.Any(l => l.Equals(targetLang, StringComparison.OrdinalIgnoreCase));
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToList();

                _logger.LogInformation("[MM] Fallback found {Count} problems.", fallbackMatches.Count);

                if (fallbackMatches.Any())
                {
                    var rand = new Random();
                    problem = fallbackMatches[rand.Next(fallbackMatches.Count)];
                    _logger.LogInformation("[MM] Fallback selected: {Title}", problem.Title);
                }
            }

            if (problem == null)
            {
                _logger.LogError("[MM] No problem found at all for lang={Lang}. Re-queuing both players.", targetLang);
                _queueManager.Enqueue(p1);
                _queueManager.Enqueue(p2);
                continue;
            }

            // 2. Initialize Battle Database Rows
            try
            {
                var battle = Battle.Create(problem.Id, p1.Difficulty);
                battle.Start();
                _context.Battles.Add(battle);

                var bp1 = BattleParticipant.Create(battle.Id, p1.UserId, p1.Rating);
                var bp2 = BattleParticipant.Create(battle.Id, p2.UserId, p2.Rating);
                _context.BattleParticipants.Add(bp1);
                _context.BattleParticipants.Add(bp2);

                await _context.SaveChangesAsync(default);
                _logger.LogInformation("[MM] Battle {BattleId} saved to DB.", battle.Id);

                // 3. Notify clients
                await Clients.Client(p1.ConnectionId).SendAsync("OpponentFound", new
                {
                    battleId = battle.Id,
                    opponentName = p2.Username,
                    opponentElo = p2.Rating,
                    problemId = problem.Id,
                    problemTitle = problem.Title,
                    language = p1.PreferredLanguage
                });

                await Clients.Client(p2.ConnectionId).SendAsync("OpponentFound", new
                {
                    battleId = battle.Id,
                    opponentName = p1.Username,
                    opponentElo = p1.Rating,
                    problemId = problem.Id,
                    problemTitle = problem.Title,
                    language = p2.PreferredLanguage
                });

                _logger.LogInformation("[MM] OpponentFound sent to both players.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MM] EXCEPTION while creating battle for {P1} vs {P2}", p1.Username, p2.Username);
            }
        }
    }
}
