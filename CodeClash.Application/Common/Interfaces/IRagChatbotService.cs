using CodeClash.Application.Features.Chatbot.Dtos;

namespace CodeClash.Application.Common.Interfaces;

/// <summary>
/// Top-level RAG orchestration service — called by the controller.
/// Coordinates structured retrieval, vector retrieval, history, and completion.
/// </summary>
public interface IRagChatbotService
{
    Task<ChatResponse> ChatAsync(Guid userId, ChatRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid sessionId, int limit = 50, CancellationToken ct = default);
}
