using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentResults;

public class GetTournamentResultsQueryHandler : IRequestHandler<GetTournamentResultsQuery, IEnumerable<TournamentResultDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTournamentResultsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TournamentResultDto>> Handle(GetTournamentResultsQuery request, CancellationToken cancellationToken)
    {
        var results = await _context.TournamentResults
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.TournamentId == request.TournamentId)
            .OrderBy(r => r.Rank)
            .ThenByDescending(r => r.TotalPoints)
            .Select(r => new TournamentResultDto
            {
                UserId = r.UserId,
                Username = r.User.Username,
                FullName = r.User.FullName,
                ProfileImageUrl = r.User.ProfileImageUrl,
                Rank = r.Rank,
                TotalPoints = r.TotalPoints,
                CompletedAt = r.CompletedAt
            })
            .ToListAsync(cancellationToken);

        return results;
    }
}
