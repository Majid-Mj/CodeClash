using CodeClash.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;
    private readonly ITournamentNotificationService _notificationService;

    public RegisterUserCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context,
        ITournamentNotificationService notificationService)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(request.TournamentId, cancellationToken);

        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.TournamentId} not found.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with Id {request.UserId} not found.");
        }

        tournament.RegisterPlayer(request.UserId, user.Rating);

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Notify admin dashboard of the live participant count increase
        await _notificationService.NotifyTournamentRegistrationChangedAsync(tournament.Id, tournament.Registrations.Count);
    }
}
