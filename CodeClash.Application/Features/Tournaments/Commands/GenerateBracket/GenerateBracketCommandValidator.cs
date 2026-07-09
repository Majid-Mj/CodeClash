using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.GenerateBracket;

public class GenerateBracketCommandValidator : AbstractValidator<GenerateBracketCommand>
{
    public GenerateBracketCommandValidator()
    {
        RuleFor(x => x.TournamentId).NotEmpty().WithMessage("Tournament Id is required.");
    }
}
