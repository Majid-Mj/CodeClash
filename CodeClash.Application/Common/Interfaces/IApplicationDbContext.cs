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
    // ── Notifications ──────────────────────────────────────────────────────────
    DbSet<Notification> Notifications { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}