namespace CodeClash.Application.Common.Interfaces;

/// <summary>
/// Converts text to a dense vector (float[]) via OpenAI embeddings.
/// </summary>
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}
