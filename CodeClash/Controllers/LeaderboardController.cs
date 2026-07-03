using CodeClash.API.Common;
using CodeClash.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/leaderboard")]
[Authorize]
[Produces("application/json")]
public class LeaderboardController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public LeaderboardController(IApplicationDbContext context)
    {
        _context = context;
    }

    public record LeaderboardUserDto(
        Guid Id,
        string Username,
        string Email,
        int Elo,
        string Country
    );

    // ─────────────────────────────────────────────────────────────
    // GET /api/v1/leaderboard
    // ─────────────────────────────────────────────────────────────
    /// <summary>Returns list of all users in the system sorted alphabetically with 0 points.</summary>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<LeaderboardUserDto[]>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(CancellationToken ct)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(ct);

        var dtos = users.Select(user => {
            return new LeaderboardUserDto(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                Elo: 0, // Hardcoded to 0 for now
                Country: "US" // Hardcoded country if user entity doesn't have it
            );
        }).ToArray();

        return Ok(ApiResponse<LeaderboardUserDto[]>.Ok(dtos, "Leaderboard retrieved successfully."));
    }
}
