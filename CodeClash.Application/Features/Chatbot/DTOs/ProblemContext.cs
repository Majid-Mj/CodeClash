namespace CodeClash.Application.Features.Chatbot.Dtos;

/// <summary>
/// Structured data fetched by Dapper from the Problems table.
/// Injected into the system prompt verbatim — no vector search needed.
/// </summary>
public class ProblemContext
{
    public string Title { get; set; } = string.Empty;
    public string StatementMarkdown { get; set; } = string.Empty;
    public string ConstraintsJson { get; set; } = "[]";
    public string AllowedLanguagesJson { get; set; } = "[]";
    public int TimeLimitMs { get; set; }
    public int MemoryLimitMb { get; set; }
    public string Difficulty { get; set; } = string.Empty;
}
