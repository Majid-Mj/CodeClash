namespace CodeClash.Application.Features.Chatbot.Dtos;

public class SeedKnowledgeRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
}
