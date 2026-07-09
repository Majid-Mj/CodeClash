using CodeClash.Application.Common.Interfaces;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.UnregisterUser;

public class UnregisterUserCommandHandler : IRequestHandler<UnregisterUserCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public UnregisterUserCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task Handle(UnregisterUserCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(request.TournamentId, cancellationToken);

        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.TournamentId} not found.");
        }

        tournament.UnregisterPlayer(request.UserId);

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
