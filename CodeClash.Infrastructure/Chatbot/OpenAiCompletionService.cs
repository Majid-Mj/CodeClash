using CodeClash.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CodeClash.Infrastructure.Chatbot;

public sealed class OpenAiCompletionService : ICompletionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;

    public OpenAiCompletionService(IConfiguration configuration)
    {
        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API Key not configured.");
        
        var rawModel = configuration["OpenAI:CompletionModel"] ?? "gemini-2.5-flash";
        // Map generic name to Gemini standard
        _model = rawModel.Contains("gpt") ? "gemini-2.5-flash" : rawModel;
        
        _maxTokens = int.TryParse(configuration["OpenAI:MaxContextTokens"], out var t) ? t : 2000;
        _httpClient = new HttpClient();
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        IEnumerable<(string Role, string Content)> history,
        string userMessage,
        CancellationToken ct = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var contents = new List<object>();

        foreach (var (role, content) in history)
        {
            contents.Add(new
            {
                role = role == "assistant" ? "model" : "user",
                parts = new[] { new { text = content } }
            });
        }

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = userMessage } }
        });

        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = contents,
            generationConfig = new
            {
                maxOutputTokens = _maxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiCompletionResponse>(cancellationToken: ct);
        return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
    }

    private class GeminiCompletionResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
