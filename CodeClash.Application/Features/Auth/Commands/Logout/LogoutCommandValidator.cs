using FluentValidation;

namespace CodeClash.Application.Features.Auth.Commands.Logout;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.Dto.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}