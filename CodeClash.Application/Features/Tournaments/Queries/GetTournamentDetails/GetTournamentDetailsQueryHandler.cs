using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentDetails;

public class GetTournamentDetailsQueryHandler : IRequestHandler<GetTournamentDetailsQuery, TournamentDetailsDto>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public GetTournamentDetailsQueryHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task<TournamentDetailsDto> Handle(GetTournamentDetailsQuery request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.Id} not found.");
        }

        var tournamentDto = new TournamentDto
        {
            Id = tournament.Id,
            Title = tournament.Title,
            Description = tournament.Description,
            StartDate = tournament.StartDate,
            EndDate = tournament.EndDate,
            MaxParticipants = tournament.MaxParticipants,
            MinRating = tournament.MinRating,
            MaxRating = tournament.MaxRating,
            Status = tournament.Status.ToString(),
            Language = tournament.Language,
            ParticipantCount = tournament.Registrations.Count,
            CreatedAt = tournament.CreatedAt
        };

        var participants = await _context.TournamentRegistrations
            .AsNoTracking()
            .Where(r => r.TournamentId == request.Id)
            .Include(r => r.User)
            .OrderBy(r => r.RegisteredAt)
            .Select(r => new ParticipantDto
            {
                UserId = r.UserId,
                Username = r.User.Username,
                FullName = r.User.FullName,
                ProfileImageUrl = r.User.ProfileImageUrl,
                Rating = r.User.Rating,
                RegisteredAt = r.RegisteredAt
            })
            .ToListAsync(cancellationToken);

        var matches = await _context.TournamentMatches
            .AsNoTracking()
            .Where(m => m.TournamentId == request.Id)
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

        var results = await _context.TournamentResults
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.TournamentId == request.Id)
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

        return new TournamentDetailsDto
        {
            Tournament = tournamentDto,
            Participants = participants,
            Matches = matches,
            Results = results
        };
    }
}
