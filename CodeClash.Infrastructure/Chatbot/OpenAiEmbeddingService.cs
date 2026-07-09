using CodeClash.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Embeddings;

namespace CodeClash.Infrastructure.Chatbot;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;

    public OpenAiEmbeddingService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey not configured.");
        var model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        _client = new EmbeddingClient(model, apiKey);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var result = await _client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return result.Value.ToFloats().ToArray();
    }
}
