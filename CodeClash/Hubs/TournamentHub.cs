using Microsoft.AspNetCore.SignalR;

namespace CodeClash.API.Hubs;

public class TournamentHub : Hub
{
    public async Task JoinTournament(string tournamentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Tournament_{tournamentId}");
    }

    public async Task LeaveTournament(string tournamentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Tournament_{tournamentId}");
    }

    public async Task JoinAdminDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AdminDashboard");
    }

    public async Task LeaveAdminDashboard()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminDashboard");
    }
}
