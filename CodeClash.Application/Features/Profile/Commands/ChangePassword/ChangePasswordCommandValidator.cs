using FluentValidation;

namespace CodeClash.Application.Features.Profile.Commands.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.Dto.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.Dto.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("New password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("New password must contain at least one special character.")
            .NotEqual(x => x.Dto.CurrentPassword).WithMessage("New password cannot be the same as the current password.");
    }
}
