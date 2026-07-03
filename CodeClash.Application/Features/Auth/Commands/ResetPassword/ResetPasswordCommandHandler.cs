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

        if (user == null)
        {
            return Result<string>.Failure("Invalid email or verification OTP code.");
        }

        // Check if the user is locked out due to too many failed OTP attempts
        if (user.IsOtpLocked())
        {
            var remainingMinutes = (int)Math.Ceiling((user.OtpLockedUntil!.Value - DateTime.UtcNow).TotalMinutes);
            return Result<string>.Failure($"Too many failed OTP attempts. Please try again in {remainingMinutes} minute(s).");
        }

        // Check if OTP has been requested at all
        if (string.IsNullOrEmpty(user.PasswordResetOtp))
        {
            return Result<string>.Failure("No OTP code has been requested for this account. Please request a new one.");
        }

        // Check if OTP has expired
        if (user.ResetOtpExpires < DateTime.UtcNow)
        {
            user.ClearPasswordResetOtp();
            await _context.SaveChangesAsync(ct);
            return Result<string>.Failure("The OTP verification code has expired. Please request a new one.");
        }

        // Check if OTP matches
        if (user.PasswordResetOtp != dto.Otp.Trim())
        {
            user.IncrementOtpFailedAttempts();
            await _context.SaveChangesAsync(ct);

            int remainingAttempts = 5 - user.OtpFailedAttempts;
            if (remainingAttempts <= 0)
            {
                return Result<string>.Failure("Too many failed OTP attempts. Your OTP has been invalidated. Please request a new one.");
            }

            return Result<string>.Failure($"Invalid OTP code. You have {remainingAttempts} attempt(s) remaining.");
        }

        // OTP is valid — reset the password
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
        user.UpdatePassword(newPasswordHash);
        
        // Clear OTP fields and reset failed attempts
        user.ClearPasswordResetOtp();

        await _context.SaveChangesAsync(ct);

        return Result<string>.Success(string.Empty, "Password reset successfully. You can now login with your new password.");
    }
}
