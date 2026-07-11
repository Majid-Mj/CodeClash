using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentParticipants;

public class GetTournamentParticipantsQueryHandler : IRequestHandler<GetTournamentParticipantsQuery, IEnumerable<ParticipantDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTournamentParticipantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ParticipantDto>> Handle(GetTournamentParticipantsQuery request, CancellationToken cancellationToken)
    {
        var participants = await _context.TournamentRegistrations
            .AsNoTracking()
            .Where(r => r.TournamentId == request.TournamentId)
            .Include(r => r.User)
            .OrderBy(r => r.RegisteredAt)
            .Select(r => new ParticipantDto
            {
                UserId = r.UserId,
                Username = r.User.Username,
                FullName = r.User.FullName,
                ProfileImageUrl = r.User.ProfileImageUrl,
                RegisteredAt = r.RegisteredAt
            })
            .ToListAsync(cancellationToken);

        return participants;
    }
}
