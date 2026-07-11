using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using CodeClash.API.Extensions;
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
[Route("api/v1/customduel")]
[Authorize]
[Produces("application/json")]
public class CustomDuelController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CustomDuelController(
        IApplicationDbContext context,
        IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public record InviteRequest(Guid HostUserId, Guid FriendUserId);
    public record AcceptRequest(Guid RoomId);
    public record DeclineRequest(Guid RoomId);
    public record ReadyRequest(Guid RoomId, Guid UserId, bool IsReady);
    public record StartRequest(Guid RoomId, string? Difficulty);
    public record UpdateSettingsRequest(Guid RoomId, string Difficulty, string Language);
    public record LeaveRequest(Guid RoomId, Guid UserId);

    // POST /api/v1/customduel/invite
    [HttpPost("invite")]
    
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InviteFriend([FromBody] InviteRequest request, CancellationToken ct)
    {
        var hostUser = await _context.Users.FindAsync(new object[] { request.HostUserId }, ct);
        var friendUser = await _context.Users.FindAsync(new object[] { request.FriendUserId }, ct);

        if (hostUser == null || friendUser == null)
        {
            return BadRequest(new { message = "Host or friend user not found." });
        }

        // Generate a random uppercase room code
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        var room = CustomDuelRoom.Create(request.HostUserId, request.FriendUserId, code);
        _context.CustomDuelRooms.Add(room);
        await _context.SaveChangesAsync(ct);

        // Send SignalR notification to the friend
        await _hubContext.Clients.User(request.FriendUserId.ToString()).SendAsync("DuelInvitationReceived", new
        {
            roomId = room.Id,
            roomCode = room.RoomCode,
            hostUserId = hostUser.Id,
            hostUsername = string.IsNullOrWhiteSpace(hostUser.FullName) ? hostUser.Username : hostUser.FullName
        }, ct);

        return Ok(new
        {
            roomId = room.Id,
            roomCode = room.RoomCode,
            status = room.Status,
            hostUserId = room.HostUserId,
            friendUserId = room.FriendUserId
        });
    }

    // POST /api/v1/customduel/accept
    [HttpPost("accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptRequest request, CancellationToken ct)
    {
        var room = await _context.CustomDuelRooms
            .Include(r => r.HostUser)
            .Include(r => r.FriendUser)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, ct);

        if (room == null)
        {
            return NotFound(new { message = "Custom duel room not found." });
        }

        if (room.Status != "Pending")
        {
            return BadRequest(new { message = $"Cannot accept invitation. Room status is '{room.Status}'." });
        }

        room.Accept();
        await _context.SaveChangesAsync(ct);

        // Notify the host
        await _hubContext.Clients.User(room.HostUserId.ToString()).SendAsync("InvitationAccepted", new
        {
            roomId = room.Id,
            friendUserId = room.FriendUserId,
            friendUsername = string.IsNullOrWhiteSpace(room.FriendUser.FullName) ? room.FriendUser.Username : room.FriendUser.FullName
        }, ct);

        return Ok(new { message = "Invitation accepted.", roomId = room.Id });
    }

    // POST /api/v1/customduel/decline
    [HttpPost("decline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeclineInvitation([FromBody] DeclineRequest request, CancellationToken ct)
    {
        var room = await _context.CustomDuelRooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct);
        if (room == null)
        {
            return NotFound(new { message = "Custom duel room not found." });
        }

        room.Decline();
        await _context.SaveChangesAsync(ct);

        // Notify the host
        await _hubContext.Clients.User(room.HostUserId.ToString()).SendAsync("InvitationDeclined", new
        {
            roomId = room.Id
        }, ct);

        return Ok(new { message = "Invitation declined." });
    }

    // GET /api/v1/customduel/{roomId}
    [HttpGet("{roomId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomDetails(Guid roomId, CancellationToken ct)
    {
        var room = await _context.CustomDuelRooms
            .AsNoTracking()
            .Include(r => r.HostUser)
            .Include(r => r.FriendUser)
            .FirstOrDefaultAsync(r => r.Id == roomId, ct);

        if (room == null)
        {
            return NotFound(new { message = "Custom duel room not found." });
        }

        return Ok(new
        {
            id = room.Id,
            roomCode = room.RoomCode,
            status = room.Status,
            hostUserId = room.HostUserId,
            hostUsername = string.IsNullOrWhiteSpace(room.HostUser.FullName) ? room.HostUser.Username : room.HostUser.FullName,
            friendUserId = room.FriendUserId,
            friendUsername = string.IsNullOrWhiteSpace(room.FriendUser.FullName) ? room.FriendUser.Username : room.FriendUser.FullName,
            isHostReady = room.IsHostReady,
            isFriendReady = room.IsFriendReady,
            selectedProblemId = room.SelectedProblemId
        });
    }

    // POST /api/v1/customduel/ready
    [HttpPost("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPlayerReady([FromBody] ReadyRequest request, CancellationToken ct)
    {
        var room = await _context.CustomDuelRooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct);
        if (room == null)
        {
            return NotFound(new { message = "Custom duel room not found." });
        }

        if (request.UserId != room.HostUserId && request.UserId != room.FriendUserId)
        {
            return BadRequest(new { message = "User does not belong to this duel room." });
        }

        room.SetPlayerReady(request.UserId, request.IsReady);
        await _context.SaveChangesAsync(ct);

        // Notify both players in the SignalR group
        await _hubContext.Clients.Group(request.RoomId.ToString()).SendAsync("PlayerReady", new
        {
            roomId = room.Id,
            userId = request.UserId,
            isReady = request.IsReady
        }, ct);

        return Ok(new
        {
            id = room.Id,
            isHostReady = room.IsHostReady,
            isFriendReady = room.IsFriendReady
        });
    }

    // POST /api/v1/customduel/start
    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartDuel([FromBody] StartRequest request, CancellationToken ct)
    {
        var room = await _context.CustomDuelRooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct);
        if (room == null)
        {
            return NotFound(new { message = "Custom duel room not found." });
        }

        // Verify ready states
        if (!room.IsHostReady || !room.IsFriendReady)
        {
            return BadRequest(new { message = "Cannot start duel. Both players must be ready." });
        }

        // Select a random active problem filtered by difficulty if provided, with fallback to any active problem if none match
        var query = _context.Problems.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Difficulty) &&
            Enum.TryParse<Difficulty>(request.Difficulty, ignoreCase: true, out var difficultyEnum))
        {
            var filteredQuery = query.Where(p => p.Difficulty == difficultyEnum);
            if (await filteredQuery.AnyAsync(ct))
            {
                query = filteredQuery;
            }
        }

        var problem = await query
            .OrderBy(p => Guid.NewGuid()) // Random order
            .FirstOrDefaultAsync(ct);

        if (problem == null)
        {
            return BadRequest(new { message = "No active problems found to launch the duel." });
        }

        room.Start(problem.Id);
        await _context.SaveChangesAsync(ct);

        // Notify all players in the SignalR group
        await _hubContext.Clients.Group(request.RoomId.ToString()).SendAsync("DuelStarted", new
        {
            roomId = room.Id,
            roomCode = room.RoomCode,
            problemId = problem.Id
        }, ct);

        // Also send directly to each user to guarantee delivery even if they
        // auto-reconnected and lost SignalR group membership
        await _hubContext.Clients.User(room.HostUserId.ToString()).SendAsync("DuelStarted", new
        {
            roomId = room.Id,
            roomCode = room.RoomCode,
            problemId = problem.Id
        }, ct);
        await _hubContext.Clients.User(room.FriendUserId.ToString()).SendAsync("DuelStarted", new
        {
            roomId = room.Id,
            roomCode = room.RoomCode,
            problemId = problem.Id
        }, ct);

        return Ok(new
        {
            roomId = room.Id,
            status = room.Status,
            problemId = problem.Id
        });
    }

    // POST /api/v1/customduel/settings
    [HttpPost("settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLobbySettings([FromBody] UpdateSettingsRequest request, CancellationToken ct)
    {
        var room = await _context.CustomDuelRooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct);
        if (room == null)
        {
            return NotFound(new { message = "Custom duel room not found." });
        }

        // Notify both players in the SignalR group of the updated settings
        await _hubContext.Clients.Group(request.RoomId.ToString()).SendAsync("LobbySettingsUpdated", new
        {
            roomId = room.Id,
            difficulty = request.Difficulty,
            language = request.Language
        }, ct);

        return Ok(new { message = "Settings updated." });
    }

    // POST /api/v1/customduel/leave
    [HttpPost("leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveDuel([FromBody] LeaveRequest request, CancellationToken ct)
    {
        var room = await _context.CustomDuelRooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct);
        if (room == null)
        {
            return NotFound(new { message = "Custom duel room not found." });
        }

        if (request.UserId != room.HostUserId && request.UserId != room.FriendUserId)
        {
            return BadRequest(new { message = "User does not belong to this duel room." });
        }

        room.SetPlayerLeft(request.UserId);

        // If both left, complete without winner
        if (room.HasHostLeft && room.HasFriendLeft)
        {
            room.Complete(Guid.Empty);
        }

        await _context.SaveChangesAsync(ct);

        // Notify all players in the SignalR group
        await _hubContext.Clients.Group(request.RoomId.ToString()).SendAsync("PlayerLeft", new
        {
            roomId = room.Id,
            userId = request.UserId
        }, ct);

        return Ok(new { message = "Player left the duel room successfully." });
    }
}
