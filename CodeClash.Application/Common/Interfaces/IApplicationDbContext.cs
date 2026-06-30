using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}