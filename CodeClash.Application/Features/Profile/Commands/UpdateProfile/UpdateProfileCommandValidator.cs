using FluentValidation;

namespace CodeClash.Application.Features.Profile.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Dto.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Length(2, 100).WithMessage("Full name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z\s'\-]+$").WithMessage("Full name may only contain letters, spaces, hyphens, and apostrophes.");

        RuleFor(x => x.Dto.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 30).WithMessage("Username must be between 3 and 30 characters.")
            .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Username can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(x => x.Dto.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^[0-9]{10}$").WithMessage("Phone number must be exactly 10 digits and contain only numbers (cannot start with alphabets or special characters).");
    }
}
