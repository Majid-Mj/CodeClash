using System;

namespace CodeClash.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty; // 'info', 'success', 'warning', 'error'
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    // EF Constructor
    private Notification() { }

    public Notification(Guid userId, string title, string message, string type)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
