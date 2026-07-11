using CodeClash.Application.Common.Interfaces;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.PublishTournament;

public class PublishTournamentCommandHandler : IRequestHandler<PublishTournamentCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public PublishTournamentCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task Handle(PublishTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.Id} not found.");
        }

        tournament.Publish();

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
