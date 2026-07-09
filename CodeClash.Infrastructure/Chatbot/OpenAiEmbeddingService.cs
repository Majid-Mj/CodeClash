using CodeClash.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CodeClash.Infrastructure.Chatbot;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAiEmbeddingService(IConfiguration configuration)
    {
        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API Key not configured.");
        
        var rawModel = configuration["OpenAI:EmbeddingModel"] ?? "gemini-embedding-001";
        // Map generic names containing 'embedding' to Gemini's standard model
        _model = rawModel.Contains("embedding") ? "gemini-embedding-001" : rawModel;
        
        _httpClient = new HttpClient();
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:embedContent?key={_apiKey}";
        
        var requestBody = new
        {
            content = new
            {
                parts = new[]
                {
                    new { text = text }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiEmbeddingResponse>(cancellationToken: ct);
        return result?.Embedding?.Values ?? Array.Empty<float>();
    }

    private class GeminiEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public GeminiEmbedding? Embedding { get; set; }
    }

    private class GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public float[]? Values { get; set; }
    }
}
