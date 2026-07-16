using CodeClash.Domain.Entities;

namespace CodeClash.Application.Common.Interfaces;

public interface ITournamentRepository
{
    Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tournament?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tournament>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Tournament tournament, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tournament tournament, CancellationToken cancellationToken = default);
    Task DeleteAsync(Tournament tournament, CancellationToken cancellationToken = default);
    Task<int> ExecuteAtomicMatchResultUpdateAsync(Guid matchId, Guid winnerId, CancellationToken cancellationToken = default);
}

