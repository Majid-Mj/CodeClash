using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournaments;

public class GetTournamentsQueryHandler : IRequestHandler<GetTournamentsQuery, IEnumerable<TournamentDto>>
{
    private readonly ITournamentRepository _tournamentRepository;

    public GetTournamentsQueryHandler(ITournamentRepository tournamentRepository)
    {
        _tournamentRepository = tournamentRepository;
    }

    public async Task<IEnumerable<TournamentDto>> Handle(GetTournamentsQuery request, CancellationToken cancellationToken)
    {
        var tournaments = await _tournamentRepository.GetAllAsync(cancellationToken);
        
        return tournaments.Select(t => new TournamentDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            MaxParticipants = t.MaxParticipants,
            MinRating = t.MinRating,
            MaxRating = t.MaxRating,
            Status = t.Status.ToString(),
            Language = t.Language,
            ParticipantCount = t.Registrations.Count,
            CreatedAt = t.CreatedAt
        });
    }
}
