using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Infrastructure.Persistence.Repositories;

public class TournamentRepository : ITournamentRepository
{
    private readonly ApplicationDbContext _context;

    public TournamentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
    
    public async Task<Tournament?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tournaments
            .Include(t => t.Registrations)
                .ThenInclude(r => r.User)
            .Include(t => t.Matches)
            .Include(t => t.Results)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Tournament>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tournaments
            .Include(t => t.Registrations)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Tournament tournament, CancellationToken cancellationToken = default)
    {
        await _context.Tournaments.AddAsync(tournament, cancellationToken);
    }

    public Task UpdateAsync(Tournament tournament, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(tournament).State == EntityState.Detached)
        {
            _context.Tournaments.Update(tournament);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Tournament tournament, CancellationToken cancellationToken = default)
    {
        _context.Tournaments.Remove(tournament);
        return Task.CompletedTask;
    }

    public async Task<int> ExecuteAtomicMatchResultUpdateAsync(Guid matchId, Guid winnerId, CancellationToken cancellationToken = default)
    {
        return await _context.Database.ExecuteSqlRawAsync(
            "UPDATE TournamentMatches SET Status = 'Completed', WinnerId = {0}, EndTime = {1} WHERE Id = {2} AND (Status = 'InProgress' OR Status = 'Live')",
            new object[] { winnerId, DateTime.UtcNow, matchId },
            cancellationToken);
    }
}

