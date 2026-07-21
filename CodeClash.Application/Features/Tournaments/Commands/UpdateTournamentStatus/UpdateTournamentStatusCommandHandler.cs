using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Enums;
using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.UpdateTournamentStatus;

public class UpdateTournamentStatusCommandHandler : IRequestHandler<UpdateTournamentStatusCommand, Unit>
{
    private readonly ITournamentRepository _tournamentRepository;

    public UpdateTournamentStatusCommandHandler(ITournamentRepository tournamentRepository)
    {
        _tournamentRepository = tournamentRepository;
    }

    public async Task<Unit> Handle(UpdateTournamentStatusCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.Id} not found.");
        }

        if (!Enum.TryParse<TournamentStatus>(request.Status, true, out var targetStatus))
        {
            throw new ArgumentException($"Invalid status string: '{request.Status}'");
        }

        switch (targetStatus)
        {
            case TournamentStatus.Published:
                tournament.Publish();
                break;
            case TournamentStatus.RegistrationOpen:
                tournament.OpenRegistration();
                break;
            case TournamentStatus.RegistrationClosed:
                tournament.CloseRegistration();
                break;
            case TournamentStatus.Live:
                tournament.Start();
                break;
            case TournamentStatus.Cancelled:
                tournament.Cancel();
                break;
            case TournamentStatus.Completed:
                tournament.Complete();
                break;
            default:
                break;
        }

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        return Unit.Value;
    }
}
