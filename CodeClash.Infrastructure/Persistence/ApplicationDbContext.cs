using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CodeClash.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ── Auth ──────────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ── Problems ──────────────────────────────────────────────────────────────
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<ProblemLanguageTemplate> ProblemLanguageTemplates => Set<ProblemLanguageTemplate>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<BattleRecord> BattleRecords => Set<BattleRecord>();
    public DbSet<Battle> Battles => Set<Battle>();
    public DbSet<BattleParticipant> BattleParticipants => Set<BattleParticipant>();

    // ── Notifications ──────────────────────────────────────────────────────────
    public DbSet<Notification> Notifications => Set<Notification>();

    // ── AI ────────────────────────────────────────────────────────────────────
    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();
    public DbSet<PromptHistory> PromptHistories => Set<PromptHistory>();
    public DbSet<AIUsageLog> AIUsageLogs => Set<AIUsageLog>();

    // ── Chatbot ──────────────────────────────────────────────────────────────
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    // ── System Logs ──────────────────────────────────────────────────────────
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    public DbSet<CustomDuelRoom> CustomDuelRooms => Set<CustomDuelRoom>();

    // ── Tournaments ───────────────────────────────────────────────────────────
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentRegistration> TournamentRegistrations => Set<TournamentRegistration>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentResult> TournamentResults => Set<TournamentResult>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : v,
                        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v));
                }
            }
        }

        base.OnModelCreating(builder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is TournamentRegistration)
                {
                    entry.State = EntityState.Added;
                }
                else if (entry.Entity is TournamentResult)
                {
                    entry.State = EntityState.Added;
                }
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}