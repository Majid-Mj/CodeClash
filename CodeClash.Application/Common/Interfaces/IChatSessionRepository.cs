using CodeClash.Application.Features.Chatbot.Dtos;
using CodeClash.Domain.Entities;

namespace CodeClash.Application.Common.Interfaces;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<ChatSession> GetOrCreateAsync(Guid userId, Guid? problemId, Guid? sessionId, CancellationToken ct = default);
    Task SaveAsync(ChatSession session, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(Guid sessionId, int limit, CancellationToken ct = default);
}
