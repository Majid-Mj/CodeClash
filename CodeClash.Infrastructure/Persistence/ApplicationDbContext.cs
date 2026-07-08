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

    // ── Problems (new) ────────────────────────────────────────────────────────
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<BattleRecord> BattleRecords => Set<BattleRecord>();

    // ── Notifications ──────────────────────────────────────────────────────────
    public DbSet<Notification> Notifications => Set<Notification>();

    // ── AI ────────────────────────────────────────────────────────────────────
    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();
    public DbSet<PromptHistory> PromptHistories => Set<PromptHistory>();
    public DbSet<AIUsageLog> AIUsageLogs => Set<AIUsageLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }
}