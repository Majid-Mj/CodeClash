using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Features.Profile.Commands.DeleteAccount;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;

    public DeleteAccountCommandHandler(IUserRepository userRepository, IFileStorageService fileStorageService)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result.Failure("User not found.", "User profile not found.");
        }

        // Delete profile picture if exists
        if (!string.IsNullOrEmpty(user.ProfileImageUrl))
        {
            try
            {
                _fileStorageService.DeleteFile(user.ProfileImageUrl);
            }
            catch
            {
                // Soft warning/ignore file deletion errors
            }
        }

        // Hard delete user from the repository (this cascade deletes refresh tokens as configured in UserConfiguration)
        await _userRepository.DeleteAsync(user, ct);

        return Result.Success("Account deleted successfully.");
    }
}
