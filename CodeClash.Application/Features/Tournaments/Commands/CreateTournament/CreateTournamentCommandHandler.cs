using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.CreateTournament;

public class CreateTournamentCommandHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;
    private readonly ITournamentNotificationService _notificationService;

    public CreateTournamentCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context,
        ITournamentNotificationService notificationService)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = Tournament.Create(
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.MaxParticipants,
            request.MinRating,
            request.MaxRating,
            request.Language);

        await _tournamentRepository.AddAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Notify eligible users within the specified rating band
        await _notificationService.NotifyTournamentCreatedAsync(tournament.Id, tournament.Title, tournament.MinRating, tournament.MaxRating);

        return tournament.Id;
    }
}
