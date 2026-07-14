using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

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
}
