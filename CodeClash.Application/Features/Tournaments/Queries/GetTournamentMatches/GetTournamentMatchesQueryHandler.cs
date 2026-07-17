using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentMatches;

public class GetTournamentMatchesQueryHandler : IRequestHandler<GetTournamentMatchesQuery, IEnumerable<MatchDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTournamentMatchesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MatchDto>> Handle(GetTournamentMatchesQuery request, CancellationToken cancellationToken)
    {
        var matches = await _context.TournamentMatches
            .AsNoTracking()
            .Where(m => m.TournamentId == request.TournamentId)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.Id)
            .Select(m => new MatchDto
            {
                Id = m.Id,
                TournamentId = m.TournamentId,
                Round = m.Round,
                Player1Id = m.Player1Id,
                Player2Id = m.Player2Id,
                WinnerId = m.WinnerId,
                BattleId = m.BattleId,
                AssignedProblemId = m.AssignedProblemId,
                Status = m.Status,
                ScheduledTime = m.ScheduledTime,
                StartTime = m.StartTime,
                EndTime = m.EndTime,
                Language = m.Tournament.Language
            })
            .ToListAsync(cancellationToken);

        return matches;
    }
}
