using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Commands.StartMatch;

public class StartMatchCommandHandler : IRequestHandler<StartMatchCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public StartMatchCommandHandler(ITournamentRepository tournamentRepository, IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task Handle(StartMatchCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(request.TournamentId, cancellationToken);
        if (tournament == null)
            throw new KeyNotFoundException($"Tournament with Id {request.TournamentId} not found.");

        var match = tournament.Matches.FirstOrDefault(m => m.Id == request.MatchId);
        if (match == null)
            throw new KeyNotFoundException($"Match with Id {request.MatchId} not found.");

        if (match.Status != MatchStatus.Upcoming)
            throw new InvalidOperationException("Match is not in upcoming status.");

        // Pick a random problem
        var problem = await _context.Problems
            .OrderBy(r => Guid.NewGuid()) // Not efficient for huge tables, but ok for this scope
            .FirstOrDefaultAsync(cancellationToken);

        if (problem == null)
            throw new InvalidOperationException("No problems found in the database to assign.");

        match.Start(problem.Id);

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
