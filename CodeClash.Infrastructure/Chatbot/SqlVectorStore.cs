using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using CodeClash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CodeClash.Infrastructure.Chatbot;

/// <summary>
/// SQL Server-backed vector store with in-memory cosine similarity search.
/// All chunks are loaded into memory once and refreshed on demand.
/// Suitable for up to ~10k chunks at project scale.
/// </summary>
public sealed class SqlVectorStore : IVectorStore
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SqlVectorStore> _logger;

    // In-memory cache: chunkId → (chunk, embedding)
    private List<(KnowledgeChunk Chunk, float[] Vector)> _cache = [];
    private bool _cacheLoaded = false;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SqlVectorStore(ApplicationDbContext db, ILogger<SqlVectorStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task UpsertAsync(KnowledgeChunk chunk, CancellationToken ct = default)
    {
        var existing = await _db.KnowledgeChunks.FindAsync([chunk.Id], ct);
        if (existing is null)
            _db.KnowledgeChunks.Add(chunk);
        else
            existing.UpdateEmbedding(chunk.EmbeddingJson);

        await _db.SaveChangesAsync(ct);

        // Invalidate in-memory cache so next search reloads
        _cacheLoaded = false;
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> SearchAsync(
        float[] queryVector, int topK = 3, CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(ct);

        return _cache
            .Select(entry => (entry.Chunk, Score: CosineSimilarity(queryVector, entry.Vector)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();
    }

    public async Task RefreshCacheAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var chunks = await _db.KnowledgeChunks.ToListAsync(ct);
            _cache = chunks
                .Select(c =>
                {
                    var vec = JsonSerializer.Deserialize<float[]>(c.EmbeddingJson) ?? [];
                    return (c, vec);
                })
                .ToList();
            _cacheLoaded = true;
            _logger.LogInformation("Vector store cache refreshed: {Count} chunks loaded.", _cache.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureCacheLoadedAsync(CancellationToken ct)
    {
        if (_cacheLoaded) return;
        await RefreshCacheAsync(ct);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0f;

        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom == 0 ? 0f : dot / denom;
    }
}
