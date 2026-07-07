using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Common.Interfaces
{
    public interface IAIProvider
    {
        /// <summary>
        /// Sends a prompt to the AI provider and returns the raw string response.
        /// </summary>
        Task<string> AnalyzeAsync(string prompt, string? systemInstruction = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Name of the provider (e.g. "Gemini", "OpenAI")
        /// </summary>
        string ProviderName { get; }
    }
}
