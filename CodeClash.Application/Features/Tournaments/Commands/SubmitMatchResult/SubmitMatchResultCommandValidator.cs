using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.SubmitMatchResult;

public class SubmitMatchResultCommandValidator : AbstractValidator<SubmitMatchResultCommand>
{
    public SubmitMatchResultCommandValidator()
    {
        RuleFor(x => x.TournamentId).NotEmpty();
        RuleFor(x => x.MatchId).NotEmpty();
        RuleFor(x => x.WinnerId).NotEmpty();
    }
}
