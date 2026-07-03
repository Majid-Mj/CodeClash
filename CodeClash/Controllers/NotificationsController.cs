using CodeClash.API.Common;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using CodeClash.API.Extensions;
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
[Route("api/v1/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public NotificationsController(IApplicationDbContext context)
    {
        _context = context;
    }

    public record NotificationDto(
        Guid Id,
        string Title,
        string Message,
        string Type,
        bool Read,
        DateTime Time
    );

    /// <summary>Returns the current user's notifications history.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto[]>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50) // Return last 50
            .ToListAsync(ct);

        var dtos = notifications.Select(n => new NotificationDto(
            n.Id,
            n.Title,
            n.Message,
            n.Type,
            n.IsRead,
            n.CreatedAt
        )).ToArray();

        return Ok(ApiResponse<NotificationDto[]>.Ok(dtos, "Notifications retrieved."));
    }

    /// <summary>Marks a specific notification as read.</summary>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

        if (notification == null)
            return NotFound(ApiResponse<object>.Fail("Notification not found."));

        notification.MarkAsRead();
        await _context.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(null, "Marked as read."));
    }

    /// <summary>Marks all notifications as read.</summary>
    [HttpPut("read-all")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
        {
            n.MarkAsRead();
        }

        if (unread.Any())
        {
            await _context.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(null, "All marked as read."));
    }
}
