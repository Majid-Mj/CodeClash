using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.DeleteTournament;

public class DeleteTournamentCommandValidator : AbstractValidator<DeleteTournamentCommand>
{
    public DeleteTournamentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Tournament Id is required.");
    }
}
