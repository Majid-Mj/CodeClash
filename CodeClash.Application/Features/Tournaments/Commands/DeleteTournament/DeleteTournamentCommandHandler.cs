using CodeClash.Application.Common.Interfaces;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.DeleteTournament;

public class DeleteTournamentCommandHandler : IRequestHandler<DeleteTournamentCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public DeleteTournamentCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task Handle(DeleteTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.Id} not found.");
        }

        await _tournamentRepository.DeleteAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
