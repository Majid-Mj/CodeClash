using CodeClash.API.Common;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Chatbot.Dtos;
using CodeClash.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/admin/knowledge")]
[Authorize(Policy = "AdminOnly")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectorStore;

    public KnowledgeBaseController(IEmbeddingService embeddings, IVectorStore vectorStore)
    {
        _embeddings = embeddings;
        _vectorStore = vectorStore;
    }

    /// <summary>Seed a new knowledge chunk. Admin only.</summary>
    [HttpPost]
    public async Task<IActionResult> Seed([FromBody] SeedKnowledgeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(ApiResponse<object>.Fail("Title and Content are required."));

        var embeddingVec = await _embeddings.EmbedAsync(request.Content, ct);
        var embeddingJson = System.Text.Json.JsonSerializer.Serialize(embeddingVec);

        var chunk = KnowledgeChunk.Create(request.Title, request.Content, embeddingJson, request.Category);
        await _vectorStore.UpsertAsync(chunk, ct);

        return Ok(ApiResponse<object>.Ok(new { id = chunk.Id }, "Knowledge chunk seeded."));
    }

    /// <summary>Force refresh the in-memory vector cache.</summary>
    [HttpPost("refresh-cache")]
    public async Task<IActionResult> RefreshCache(CancellationToken ct)
    {
        await _vectorStore.RefreshCacheAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "Vector cache refreshed."));
    }
}
