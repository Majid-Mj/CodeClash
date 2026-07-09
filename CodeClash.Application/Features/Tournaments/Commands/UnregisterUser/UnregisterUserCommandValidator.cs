using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.UnregisterUser;

public class UnregisterUserCommandValidator : AbstractValidator<UnregisterUserCommand>
{
    public UnregisterUserCommandValidator()
    {
        RuleFor(x => x.TournamentId).NotEmpty().WithMessage("Tournament Id is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}
