using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.TournamentId).NotEmpty().WithMessage("Tournament Id is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}
