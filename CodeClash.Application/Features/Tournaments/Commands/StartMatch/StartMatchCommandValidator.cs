using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.StartMatch;

public class StartMatchCommandValidator : AbstractValidator<StartMatchCommand>
{
    public StartMatchCommandValidator()
    {
        RuleFor(x => x.TournamentId).NotEmpty();
        RuleFor(x => x.MatchId).NotEmpty();
    }
}
