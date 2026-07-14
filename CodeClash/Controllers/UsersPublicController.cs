using CodeClash.Application.Common.Interfaces;
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
[Route("api/v1/users")]
[Authorize]
[Produces("application/json")]
public class UsersPublicController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public UsersPublicController(IApplicationDbContext context)
    {
        _context = context;
    }

    public record UserSearchResultDto(
        Guid Id,
        string Username,
        string Email,
        string? ProfilePicture
    );

    // GET /api/v1/users/search
    [HttpGet("search")]
    [ProducesResponseType(typeof(UserSearchResultDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchUsers([FromQuery] string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(Array.Empty<UserSearchResultDto>());
        }

        Guid currentUserId = User.GetUserId();
        var searchLower = query.Trim().ToLower();

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Id != currentUserId && 
                       (u.Username.Contains(searchLower) || u.Email.Contains(searchLower) || u.FullName.Contains(searchLower)))
            .Take(15)
            .ToListAsync(ct);

        var results = users.Select(u => new UserSearchResultDto(
            Id: u.Id,
            Username: string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName,
            Email: u.Email,
            ProfilePicture: u.ProfileImageUrl
        )).ToArray();

        return Ok(results);
    }
}
