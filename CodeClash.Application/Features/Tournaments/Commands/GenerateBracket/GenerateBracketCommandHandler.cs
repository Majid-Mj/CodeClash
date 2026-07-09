using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Tournaments.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.GenerateBracket;

public class GenerateBracketCommandHandler : IRequestHandler<GenerateBracketCommand, IEnumerable<MatchDto>>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public GenerateBracketCommandHandler(ITournamentRepository tournamentRepository, IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task<IEnumerable<MatchDto>> Handle(GenerateBracketCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(request.TournamentId, cancellationToken);

        if (tournament == null)
            throw new KeyNotFoundException($"Tournament with Id {request.TournamentId} not found.");

        tournament.GenerateBracket();

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return tournament.Matches.Select(m => new MatchDto
        {
            Id = m.Id,
            TournamentId = m.TournamentId,
            Round = m.Round,
            Player1Id = m.Player1Id,
            Player2Id = m.Player2Id,
            WinnerId = m.WinnerId,
            Status = m.Status,
            StartTime = m.StartTime,
            EndTime = m.EndTime
        }).ToList();
    }
}
