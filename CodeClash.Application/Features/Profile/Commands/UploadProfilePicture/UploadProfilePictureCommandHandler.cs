using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Features.Profile.Commands.UploadProfilePicture;

public class UploadProfilePictureCommandHandler : IRequestHandler<UploadProfilePictureCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;

    public UploadProfilePictureCommandHandler(IUserRepository userRepository, IFileStorageService fileStorageService)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<string>> Handle(UploadProfilePictureCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result<string>.Failure("User not found.", "User profile not found.");
        }

        // Delete old profile picture if exists
        if (!string.IsNullOrEmpty(user.ProfileImageUrl))
        {
            try
            {
                _fileStorageService.DeleteFile(user.ProfileImageUrl);
            }
            catch
            {
                // Soft warning/ignore file deletion errors to prevent breaking the flow
            }
        }

        // Save new profile picture
        string imageUrl = await _fileStorageService.SaveFileAsync(request.FileStream, request.FileName, "profiles", ct);

        // Update user state
        user.UpdateProfileImageUrl(imageUrl);

        // Persist update
        await _userRepository.UpdateAsync(user, ct);

        return Result<string>.Success(imageUrl, "Profile picture uploaded successfully.");
    }
}
