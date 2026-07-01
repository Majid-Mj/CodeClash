using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Features.Profile.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;

    public ChangePasswordCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result.Failure("User not found.", "User profile not found.");
        }

        // Verify current password
        bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.Dto.CurrentPassword, user.PasswordHash);
        if (!isPasswordCorrect)
        {
            return Result.Failure("Incorrect current password.", "Incorrect current password.");
        }

        // Hash new password using BCrypt
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Dto.NewPassword, workFactor: 12);

        // Update password
        user.UpdatePassword(newPasswordHash);

        // Persist update
        await _userRepository.UpdateAsync(user, ct);

        return Result.Success("Password changed successfully.");
    }
}
