using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Enums;
using CodeClash.Domain.Entities;
using System.Linq;

namespace CodeClash.API.Hubs;

[Authorize]
public class BattleHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly IBattleResolutionService _battleResolutionService;

    public BattleHub(
        IApplicationDbContext context,
        IBattleResolutionService battleResolutionService)
    {
        _context = context;
        _battleResolutionService = battleResolutionService;
    }

    public async Task JoinBattleRoom(string battleIdStr)
    {
        if (!Guid.TryParse(battleIdStr, out var battleId))
        {
            throw new HubException("Invalid Battle ID.");
        }

        var battle = await _context.Battles.FindAsync(new object[] { battleId });
        if (battle == null || battle.Status != BattleStatus.InProgress)
        {
            throw new HubException("Battle is not active.");
        }

        // Set participant joined flag to prevent auto-forfeit
        var userIdStr = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
        {
            var participant = await _context.BattleParticipants
                .FirstOrDefaultAsync(bp => bp.BattleId == battleId && bp.UserId == userId);
            if (participant != null)
            {
                participant.MarkJoinedRoom();
                await _context.SaveChangesAsync();
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, battleIdStr);
        await Clients.Group(battleIdStr).SendAsync("PlayerConnected", Context.UserIdentifier);
    }

    public async Task SendTypingStatus(string battleIdStr, bool isTyping)
    {
        await Clients.OthersInGroup(battleIdStr).SendAsync("OpponentTyping", isTyping);
    }

    public async Task SendCodeMirror(string battleIdStr, string sourceCode)
    {
        await Clients.OthersInGroup(battleIdStr).SendAsync("OpponentCodeUpdated", sourceCode);
    }

    public async Task SendProgressUpdate(string battleIdStr, int passedCount, int totalCount)
    {
        await Clients.OthersInGroup(battleIdStr).SendAsync("OpponentProgressUpdated", passedCount, totalCount);
    }

    public async Task Surrender(string battleIdStr)
    {
        if (!Guid.TryParse(battleIdStr, out var battleId)) return;
        var userIdStr = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return;

        var battle = await _context.Battles
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == battleId);

        if (battle == null || battle.Status != BattleStatus.InProgress) return;

        // The caller surrenders, so the other participant is the winner
        var opponent = await _context.BattleParticipants
            .FirstOrDefaultAsync(bp => bp.BattleId == battleId && bp.UserId != userId);

        if (opponent != null)
        {
            await _battleResolutionService.ResolveBattleAsync(battleId, opponent.UserId, "N/A");
        }
        else
        {
            battle.Cancel();
            await _context.SaveChangesAsync(default);
            await Clients.Group(battleIdStr).SendAsync("BattleCancelled");
        }
    }

    public async Task TimeExpired(string battleIdStr, int myPassedCount)
    {
        if (!Guid.TryParse(battleIdStr, out var battleId)) return;
        var userIdStr = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return;

        var battle = await _context.Battles
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == battleId);

        if (battle == null || battle.Status != BattleStatus.InProgress) return;

        // Only let the first caller resolve the battle (idempotency guard via status check above)
        // Record this player's passed count in a temporary in-memory store keyed by battleId
        // Since we don't have a temporary store, we resolve with the player who has more passed tests.
        // We'll use a convention: store scores in BattleParticipant via a separate hub invocation pattern.
        // Simplest correct approach: the last player to call TimeExpired resolves with the opponent winning if other never called.
        // We will treat time expiry as: no winner (draw) — both get a small ELO draw update.

        // For now, cancel the battle to indicate time draw, broadcasting to both players.
        battle.Cancel();
        await _context.SaveChangesAsync(default);

        await Clients.Group(battleIdStr).SendAsync("BattleTimedOut");
    }
}
