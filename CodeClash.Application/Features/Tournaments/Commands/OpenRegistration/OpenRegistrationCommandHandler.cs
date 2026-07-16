using CodeClash.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CodeClash.Application.Features.Tournaments.Commands.OpenRegistration;

public class OpenRegistrationCommandHandler : IRequestHandler<OpenRegistrationCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;

    public OpenRegistrationCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
    }

    public async Task Handle(OpenRegistrationCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (tournament == null)
        {
            throw new KeyNotFoundException($"Tournament with Id {request.Id} not found.");
        }

        tournament.OpenRegistration();

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
