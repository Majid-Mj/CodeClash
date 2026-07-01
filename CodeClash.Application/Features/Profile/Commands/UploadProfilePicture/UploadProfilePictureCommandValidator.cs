using FluentValidation;
using System.IO;
using System.Linq;

namespace CodeClash.Application.Features.Profile.Commands.UploadProfilePicture;

public class UploadProfilePictureCommandValidator : AbstractValidator<UploadProfilePictureCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public UploadProfilePictureCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .Must(fileName =>
            {
                if (string.IsNullOrEmpty(fileName)) return false;
                var ext = Path.GetExtension(fileName);
                return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext.ToLower());
            }).WithMessage($"Invalid file extension. Allowed extensions are: {string.Join(", ", AllowedExtensions)}");

        RuleFor(x => x.FileLength)
            .GreaterThan(0).WithMessage("File content is empty.")
            .LessThanOrEqualTo(MaxFileSize).WithMessage("File size exceeds the limit of 5MB.");
    }
}
