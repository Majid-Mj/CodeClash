using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.PublishTournament;

public class PublishTournamentCommandValidator : AbstractValidator<PublishTournamentCommand>
{
    public PublishTournamentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Tournament Id is required.");
    }
}
