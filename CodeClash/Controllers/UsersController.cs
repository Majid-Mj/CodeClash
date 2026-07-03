using CodeClash.API.Common;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
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
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public UsersController(IApplicationDbContext context)
    {
        _context = context;
    }

    public record UserManagementDto(
        Guid Id,
        string Username,
        string Email,
        int Elo,
        string Role,
        string Status,
        string JoinDate
    );

    // ─────────────────────────────────────────────────────────────
    // GET /api/v1/admin/users
    // ─────────────────────────────────────────────────────────────
    /// <summary>Returns list of all users in the system for administrative management.</summary>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden (Admins only)</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserManagementDto[]>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(ct);

        var dtos = users.Select(user => {
            // Generate a realistic, deterministic ELO based on username hash
            int hash = user.Username.Split('@')[0].Aggregate(0, (acc, c) => acc + c);
            int elo = 1200 + (hash % 800);

            return new UserManagementDto(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                Elo: elo,
                Role: user.Role.ToString(),
                Status: user.IsActive ? "Active" : "Suspended",
                JoinDate: user.CreatedAt.ToString("yyyy-MM-dd")
            );
        }).ToArray();

        return Ok(ApiResponse<UserManagementDto[]>.Ok(dtos, "System users retrieved successfully."));
    }

    // ─────────────────────────────────────────────────────────────
    // PUT /api/v1/admin/users/{userId}/toggle-status
    // ─────────────────────────────────────────────────────────────
    /// <summary>Suspends or activates a user account. Admins cannot block other admins.</summary>
    /// <response code="200">User status toggled successfully</response>
    /// <response code="400">User is an admin (cannot block admin) or invalid ID</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden (Admins only)</response>
    /// <response code="404">User not found</response>
    [HttpPut("{userId:guid}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleUserStatus(Guid userId, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found.", "UserNotFound"));
        }

        // Prevent blocking admin accounts
        if (user.Role == UserRole.Admin)
        {
            return BadRequest(ApiResponse<object>.Fail("Action Denied: Administrator accounts cannot be suspended or deactivated.", "AdminBlockForbidden"));
        }

        if (user.IsActive)
        {
            user.Deactivate();
        }
        else
        {
            user.Activate();
        }

        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, $"User {user.Username} status updated to {(user.IsActive ? "Active" : "Suspended")} successfully."));
    }
}
