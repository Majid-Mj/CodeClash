using CodeClash.Application.Common.Interfaces;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.SubmitMatchResult;

public class SubmitMatchResultCommandHandler : IRequestHandler<SubmitMatchResultCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public SubmitMatchResultCommandHandler(ITournamentRepository tournamentRepository, IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task Handle(SubmitMatchResultCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(request.TournamentId, cancellationToken);
        if (tournament == null)
            throw new KeyNotFoundException($"Tournament with Id {request.TournamentId} not found.");

        tournament.SubmitMatchResult(request.MatchId, request.WinnerId);

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
