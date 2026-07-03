using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CodeClash.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Dto.Email.ToLower(), ct);

        // Security best practice: Always return a success message even if the user does not exist.
        // This prevents database user enumeration attacks.
        if (user == null)
        {
            return Result<string>.Success(string.Empty, "If your email is registered, you will receive an OTP code shortly.");
        }

        // Generate a 6-digit secure numeric OTP (e.g. 100000 to 999999)
        int otpValue = RandomNumberGenerator.GetInt32(100000, 1000000);
        string otp = otpValue.ToString();
        DateTime expires = DateTime.UtcNow.AddMinutes(10); // OTP expires in 10 minutes

        // Save OTP to database
        user.SetPasswordResetOtp(otp, expires);
        await _context.SaveChangesAsync(ct);

        // Send OTP email
        await _emailService.SendPasswordResetOtpAsync(user.Email, user.FullName, otp, ct);

        return Result<string>.Success(string.Empty, "If your email is registered, you will receive an OTP code shortly.");
    }
}
