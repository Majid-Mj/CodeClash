using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CodeClash.API.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Resolve sub or name identifier from JWT claims principal
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? connection.User?.FindFirst("sub")?.Value;
    }
}
