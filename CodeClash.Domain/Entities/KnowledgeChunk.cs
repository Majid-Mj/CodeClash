namespace CodeClash.Domain.Entities;

/// <summary>
/// A chunk of knowledge stored in the vector knowledge base.
/// The Embedding column holds the serialised float[] from OpenAI.
/// </summary>
public class KnowledgeChunk
{
    public Guid Id { get; private set; }

    /// <summary>Short descriptive title (used in sourcesUsed[]).</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Raw text content that was embedded.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// JSON-serialised float[] from OpenAI text-embedding-3-small.
    /// Stored as nvarchar(max) — simple, no SQL Server vector extension required.
    /// </summary>
    public string EmbeddingJson { get; private set; } = "[]";

    /// <summary>Optional tag to group chunks (e.g. "algorithms", "data-structures").</summary>
    public string? Category { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF constructor
    private KnowledgeChunk() { }

    public static KnowledgeChunk Create(string title, string content, string embeddingJson, string? category = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Content = content.Trim(),
            EmbeddingJson = embeddingJson,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void UpdateEmbedding(string embeddingJson)
    {
        EmbeddingJson = embeddingJson;
        UpdatedAt = DateTime.UtcNow;
    }
}
