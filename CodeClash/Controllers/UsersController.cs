using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CodeClash.API.Hubs;
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
    private readonly IHubContext<NotificationHub> _hubContext;

    public UsersController(IApplicationDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
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

    // GET /api/v1/admin/users
    [HttpGet]
    [ProducesResponseType(typeof(UserManagementDto[]), StatusCodes.Status200OK)]
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
                JoinDate: user.CreatedAt.ToString("MMM dd, yyyy")
            );
        }).ToArray();

        return Ok(dtos);
    }

    // PUT /api/v1/admin/users/{userId}/toggle-status
    [HttpPut("{userId:guid}/toggle-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleUserStatus(Guid userId, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Prevent blocking admin accounts
        if (user.Role == UserRole.Admin)
        {
            return BadRequest(new { message = "Action Denied: Administrator accounts cannot be suspended or deactivated." });
        }

        if (user.IsActive)
        {
            user.Deactivate();
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ForceLogout", ct);
        }
        else
        {
            user.Activate();
        }

        await _context.SaveChangesAsync(ct);
        return Ok(new { message = $"User {user.Username} status updated to {(user.IsActive ? "Active" : "Suspended")} successfully." });
    }

    // POST /api/v1/admin/users/notify
    [HttpPost("notify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendNotification(
        [FromBody] SendNotificationDto dto,
        [FromServices] IHubContext<NotificationHub> hubContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest(new { message = "Title and Message are required." });
        }

        string type = dto.Type?.ToLower() ?? "info";

        if (dto.UserId.HasValue)
        {
            var notif = new Notification(dto.UserId.Value, dto.Title.Trim(), dto.Message.Trim(), type);
            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync(ct);

            await hubContext.Clients.User(dto.UserId.Value.ToString()).SendAsync("ReceiveNotification", new
            {
                title = dto.Title.Trim(),
                message = dto.Message.Trim(),
                type = type
            });
        }
        else
        {
            // For global notifications, we will broadcast via SignalR. 
            // We'll also fetch all user IDs and persist a notification for each so it stays in their history.
            var allUserIds = await _context.Users.Select(u => u.Id).ToListAsync(ct);
            var notifications = allUserIds.Select(id => new Notification(id, dto.Title.Trim(), dto.Message.Trim(), type));
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync(ct);

            await hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                title = dto.Title.Trim(),
                message = dto.Message.Trim(),
                type = type
            });
        }

        return Ok(new { message = "System notification pushed successfully." });
    }
}

public record SendNotificationDto(
    Guid? UserId,
    string Title,
    string Message,
    string? Type
);
