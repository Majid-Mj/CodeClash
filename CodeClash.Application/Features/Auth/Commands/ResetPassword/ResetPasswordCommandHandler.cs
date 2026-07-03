using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;

    public ResetPasswordCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower(), ct);

        if (user == null || user.PasswordResetOtp != dto.Otp.Trim())
        {
            return Result<string>.Failure("Invalid email or verification OTP code.");
        }

        if (user.ResetOtpExpires < DateTime.UtcNow)
        {
            return Result<string>.Failure("The OTP verification code has expired.");
        }

        // Hash new password using BCrypt
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
        user.UpdatePassword(newPasswordHash);
        
        // Clear OTP fields
        user.ClearPasswordResetOtp();

        await _context.SaveChangesAsync(ct);

        return Result<string>.Success(string.Empty, "Password reset successfully. You can now login with your new password.");
    }
}
