using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Enums;
using System.Linq;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/matchmaking")]
[Authorize]
public class MatchmakingController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public MatchmakingController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("queue")]
    public IActionResult CreateQueueTicket()
    {
        // Generates an authorization ticket identifier clients can pass to SignalR
        var queueId = Guid.NewGuid();
        return Ok(new { queueId });
    }

    [HttpGet("battle/{id}")]
    public async Task<IActionResult> GetBattleDetails(Guid id)
    {
        var battle = await _context.Battles
            .Include(b => b.Participants)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (battle == null)
        {
            return NotFound("Battle not found.");
        }

        int durationSeconds = 1800; // default 30 mins
        var diff = battle.Difficulty;
        if (diff == Difficulty.Easy) durationSeconds = 600;
        else if (diff == Difficulty.Medium) durationSeconds = 900;
        else if (diff == Difficulty.Hard) durationSeconds = 1500;

        double timeRemainingSeconds = durationSeconds;
        if (battle.StartTime.HasValue)
        {
            var elapsed = (DateTime.UtcNow - battle.StartTime.Value).TotalSeconds;
            timeRemainingSeconds = Math.Max(0, durationSeconds - elapsed);
        }

        var participants = battle.Participants.Select(p => new
        {
            userId = p.UserId,
            username = p.User?.Username ?? p.User?.FullName ?? "Player",
            rating = p.RatingBefore
        }).ToList();

        return Ok(new
        {
            battleId = battle.Id,
            status = battle.Status.ToString(),
            startTime = battle.StartTime,
            durationSeconds,
            timeRemainingSeconds = (int)Math.Floor(timeRemainingSeconds),
            participants
        });
    }
}
