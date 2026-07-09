using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.CancelTournament;

public class CancelTournamentCommandValidator : AbstractValidator<CancelTournamentCommand>
{
    public CancelTournamentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Tournament Id is required.");
    }
}
