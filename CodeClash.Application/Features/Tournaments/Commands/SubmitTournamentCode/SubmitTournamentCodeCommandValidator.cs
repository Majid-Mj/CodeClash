using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.SubmitTournamentCode;

public class SubmitTournamentCodeCommandValidator : AbstractValidator<SubmitTournamentCodeCommand>
{
    public SubmitTournamentCodeCommandValidator()
    {
        RuleFor(x => x.TournamentId).NotEmpty();
        RuleFor(x => x.MatchId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.SourceCode).NotEmpty();
    }
}
