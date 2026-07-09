namespace CodeClash.Domain.Entities;

/// <summary>
/// A single turn in a chat session. Role is "user" or "assistant".
/// </summary>
public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }

    /// <summary>"user" or "assistant"</summary>
    public string Role { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public ChatSession Session { get; private set; } = null!;

    // EF constructor
    private ChatMessage() { }

    internal static ChatMessage Create(Guid sessionId, string role, string content)
        => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
}
