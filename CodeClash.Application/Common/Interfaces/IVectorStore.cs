using CodeClash.Domain.Entities;

namespace CodeClash.Application.Common.Interfaces;

/// <summary>
/// Vector store for knowledge chunks.
/// Default implementation: SQL Server + in-memory cosine similarity.
/// </summary>
public interface IVectorStore
{
    /// <summary>Upsert a knowledge chunk (embed → store).</summary>
    Task UpsertAsync(KnowledgeChunk chunk, CancellationToken ct = default);

    /// <summary>
    /// Returns the top-K chunks most similar to the query vector.
    /// </summary>
    Task<IReadOnlyList<KnowledgeChunk>> SearchAsync(float[] queryVector, int topK = 3, CancellationToken ct = default);

    /// <summary>Reload the in-memory cache from the database.</summary>
    Task RefreshCacheAsync(CancellationToken ct = default);
}
