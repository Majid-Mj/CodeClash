using CodeClash.API.Common;
using CodeClash.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminDashboardController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AdminDashboardController(IApplicationDbContext context)
    {
        _context = context;
    }

    public record DashboardStatsDto(
        int TotalUsers,
        int ActiveMatches,
        double SubmissionRate,
        int SystemLoad
    );

    /// <summary>Returns the current dashboard analytics statistics.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var totalUsers = await _context.Users.CountAsync(ct);
        
        // Placeholder values as requested by the user until we have actual database structures for these
        var activeMatches = 0;
        var submissionRate = 0.0;
        var systemLoad = 0;

        var stats = new DashboardStatsDto(
            totalUsers,
            activeMatches,
            submissionRate,
            systemLoad
        );

        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats, "Dashboard stats retrieved successfully."));
    }
}
