using Microsoft.AspNetCore.SignalR;

namespace CodeClash.API.Hubs;

public class NotificationHub : Hub
{
    // Hub interface for push notifications. Client listens to "ReceiveNotification".

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
    }
}
