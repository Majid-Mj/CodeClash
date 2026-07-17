using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CodeClash.Application.Common.Interfaces;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/matchmaking")]
[Authorize]
public class MatchmakingController : ControllerBase
{
    [HttpPost("queue")]
    public IActionResult CreateQueueTicket()
    {
        // Generates an authorization ticket identifier clients can pass to SignalR
        var queueId = Guid.NewGuid();
        return Ok(new { queueId });
    }

    [HttpGet("battle/{battleId:guid}")]
    public async Task<IActionResult> GetBattleById(Guid battleId, [FromServices] IApplicationDbContext context)
    {
        var battle = await context.Battles
            .Include(b => b.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(b => b.Id == battleId);

        if (battle == null)
        {
            return NotFound(new { message = "Battle not found." });
        }

        // Calculate timeRemainingSeconds
        int durationSeconds = 30 * 60; // default 30 mins
        var diff = battle.Difficulty.ToString().ToLower();
        if (diff == "easy") durationSeconds = 10 * 60;
        else if (diff == "medium") durationSeconds = 15 * 60;
        else if (diff == "hard") durationSeconds = 25 * 60;

        int timeRemainingSeconds = durationSeconds;
        if (battle.StartTime.HasValue)
        {
            var elapsed = (DateTime.UtcNow - battle.StartTime.Value).TotalSeconds;
            timeRemainingSeconds = durationSeconds - (int)elapsed;
            if (timeRemainingSeconds < 0) timeRemainingSeconds = 0;
        }

        return Ok(new
        {
            id = battle.Id,
            status = battle.Status.ToString(),
            problemId = battle.ProblemId,
            durationSeconds = durationSeconds,
            timeRemainingSeconds = timeRemainingSeconds,
            mode = battle.Mode,
            participants = battle.Participants.Select(p => new
            {
                userId = p.UserId,
                username = p.User != null ? p.User.Username : "Unknown",
                rating = p.RatingBefore
            }).ToList()
        });
    }
}
