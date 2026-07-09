namespace CodeClash.Application.Common.Interfaces;

/// <summary>
/// Sends a fully assembled prompt to the LLM and returns the reply.
/// </summary>
public interface ICompletionService
{
    Task<string> CompleteAsync(string systemPrompt, IEnumerable<(string Role, string Content)> history, string userMessage, CancellationToken ct = default);
}
