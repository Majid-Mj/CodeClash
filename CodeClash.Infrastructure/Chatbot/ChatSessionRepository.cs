using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Chatbot.Dtos;
using CodeClash.Domain.Entities;
using CodeClash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Infrastructure.Chatbot;

public sealed class ChatSessionRepository : IChatSessionRepository
{
    private readonly ApplicationDbContext _db;

    public ChatSessionRepository(ApplicationDbContext db) => _db = db;

    public async Task<ChatSession?> GetByIdAsync(Guid sessionId, CancellationToken ct = default)
        => await _db.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public async Task<ChatSession> GetOrCreateAsync(
        Guid userId, Guid? problemId, Guid? sessionId, CancellationToken ct = default)
    {
        if (sessionId.HasValue)
        {
            var existing = await _db.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.UserId == userId, ct);

            if (existing is not null)
            {
                existing.Touch();
                return existing;
            }
        }

        var session = ChatSession.Create(userId, problemId);
        _db.ChatSessions.Add(session);
        return session;
    }

    public async Task SaveAsync(ChatSession session, CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(
        Guid sessionId, int limit, CancellationToken ct = default)
    {
        return await _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(ct);
    }
}
