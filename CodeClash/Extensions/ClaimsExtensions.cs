using System.Security.Claims;

namespace CodeClash.API.Extensions;

public static class ClaimsExtensions
{
    /// <summary>
    /// Extracts the authenticated user's ID from the JWT 'sub' claim.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue("sub");

        if (Guid.TryParse(sub, out var userId))
            return userId;

        throw new UnauthorizedAccessException("User identity could not be resolved from token.");
    }
}