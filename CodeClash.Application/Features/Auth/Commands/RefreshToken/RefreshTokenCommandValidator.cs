using FluentValidation;

namespace CodeClash.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Dto.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
