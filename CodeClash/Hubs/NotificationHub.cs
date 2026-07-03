using Microsoft.AspNetCore.SignalR;

namespace CodeClash.API.Hubs;

public class NotificationHub : Hub
{
    // Hub interface for push notifications. Client listens to "ReceiveNotification".
}
