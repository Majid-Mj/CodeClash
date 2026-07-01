using CodeClash.Application.Features.Auth.Commands.Register;
using FluentValidation;

namespace CodeClash.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Dto.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Length(2, 100).WithMessage("Full name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z\s'\-]+$").WithMessage("Full name may only contain letters, spaces, hyphens, and apostrophes.");

        RuleFor(x => x.Dto.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .Must(email => string.IsNullOrEmpty(email) || email == email.ToLower()).WithMessage("Email must contain only lowercase letters.");

        RuleFor(x => x.Dto.PhoneNumber)
            .Matches(@"^[0-9]{10}$").WithMessage("Phone number must be exactly 10 digits and contain only numbers (cannot start with alphabets or special characters).")
            .When(x => !string.IsNullOrEmpty(x.Dto.PhoneNumber));

        RuleFor(x => x.Dto.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.Dto.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password.")
            .Equal(x => x.Dto.Password).WithMessage("Passwords do not match.");
    }
}