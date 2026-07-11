namespace CodeClash.Application.Features.Chatbot.Dtos;

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public Guid SessionId { get; set; }

    /// <summary>Titles of knowledge chunks that were included in the prompt.</summary>
    public List<string> SourcesUsed { get; set; } = [];
}
