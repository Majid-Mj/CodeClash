using CodeClash.Application.Common.Interfaces;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.CancelTournament;

public class CancelTournamentCommandHandler : IRequestHandler<CancelTournamentCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public CancelTournamentCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task Handle(CancelTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.Id} not found.");
        }

        tournament.Cancel();

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
