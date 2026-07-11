using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    // ── Problems (new) ────────────────────────────────────────────────────────
    DbSet<Problem> Problems { get; }
    DbSet<TestCase> TestCases { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<BattleRecord> BattleRecords { get; }
    // ── Notifications ──────────────────────────────────────────────────────────
    DbSet<Notification> Notifications { get; }
    
    // ── AI ────────────────────────────────────────────────────────────────────
    DbSet<AIAnalysis> AIAnalyses { get; }
    DbSet<PromptHistory> PromptHistories { get; }
    DbSet<AIUsageLog> AIUsageLogs { get; }

    // ── Chatbot ──────────────────────────────────────────────────────────────
    DbSet<KnowledgeChunk> KnowledgeChunks { get; }
    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ChatMessage> ChatMessages { get; }

    // ── System Logs ──────────────────────────────────────────────────────────
    DbSet<SystemLog> SystemLogs { get; }

    DbSet<CustomDuelRoom> CustomDuelRooms { get; }
    
    // ── Tournaments ───────────────────────────────────────────────────────────
    DbSet<Tournament> Tournaments { get; }
    DbSet<TournamentRegistration> TournamentRegistrations { get; }
    DbSet<TournamentMatch> TournamentMatches { get; }
    DbSet<TournamentResult> TournamentResults { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}