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

    // GET /api/v1/leaderboard
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LeaderboardUserDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(CancellationToken ct)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.Rating)
            .ThenBy(u => u.Username)
            .Take(100)
            .ToListAsync(ct);

        var dtos = users.Select(user => {
            return new LeaderboardUserDto(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                Elo: user.Rating, 
                Country: "US" 
            );
        }).ToArray();

        return Ok(dtos);
    }
}
