using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.CreateTournament;

public class CreateTournamentCommandHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public CreateTournamentCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = Tournament.Create(
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.MaxParticipants);

        await _tournamentRepository.AddAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return tournament.Id;
    }
}
