using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Queries.GetTournamentById;

public class GetTournamentByIdQueryHandler : IRequestHandler<GetTournamentByIdQuery, TournamentDto>
{
    private readonly ITournamentRepository _tournamentRepository;

    public GetTournamentByIdQueryHandler(ITournamentRepository tournamentRepository)
    {
        _tournamentRepository = tournamentRepository;
    }

    public async Task<TournamentDto> Handle(GetTournamentByIdQuery request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.Id} not found.");
        }
        
        return new TournamentDto
        {
            Id = tournament.Id,
            Title = tournament.Title,
            Description = tournament.Description,
            StartDate = tournament.StartDate,
            EndDate = tournament.EndDate,
            MaxParticipants = tournament.MaxParticipants,
            Status = tournament.Status.ToString(),
            CreatedAt = tournament.CreatedAt
        };
    }
}
