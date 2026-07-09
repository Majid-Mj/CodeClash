namespace CodeClash.Domain.Entities;

/// <summary>
/// Represents a chat thread between a user and the RAG chatbot.
/// A user may have multiple sessions (one per battle/problem context).
/// </summary>
public class ChatSession
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Optional problem this session is scoped to.</summary>
    public Guid? ProblemId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime LastActiveAt { get; private set; }

    private readonly List<ChatMessage> _messages = [];
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    // EF constructor
    private ChatSession() { }

    public static ChatSession Create(Guid userId, Guid? problemId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProblemId = problemId,
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };

    public ChatMessage AddUserMessage(string content)
    {
        var msg = ChatMessage.Create(Id, "user", content);
        _messages.Add(msg);
        LastActiveAt = DateTime.UtcNow;
        return msg;
    }

    public ChatMessage AddAssistantMessage(string content)
    {
        var msg = ChatMessage.Create(Id, "assistant", content);
        _messages.Add(msg);
        LastActiveAt = DateTime.UtcNow;
        return msg;
    }

    public void Touch() => LastActiveAt = DateTime.UtcNow;
}
