using FluentValidation;

namespace CodeClash.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Dto.EmailOrUsername)
            .NotEmpty().WithMessage("Email or username is required.");

        RuleFor(x => x.Dto.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}