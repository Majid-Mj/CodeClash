namespace CodeClash.Application.Features.Chatbot.Dtos;

public class ChatRequest
{
    /// <summary>The user's message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional — scopes structured retrieval to a specific problem.
    /// If null, only vector retrieval is used.
    /// </summary>
    public Guid? ProblemId { get; set; }

    /// <summary>
    /// Pass the existing session ID to continue a conversation.
    /// If null, a new session is created.
    /// </summary>
    public Guid? SessionId { get; set; }
}
