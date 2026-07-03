using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace CodeClash.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IConfiguration config)
    {
        _context = context;
        _emailService = emailService;
        _config = config;
    }

    public async Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Dto.Email.ToLower(), ct);

        // Security best practice: Always return a success message even if the user does not exist.
        // This prevents database user enumeration attacks.
        if (user == null)
        {
            return Result<string>.Success(null, "If your email is registered, you will receive a reset password link shortly.");
        }

        // Generate a cryptographically secure 32-character token
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        DateTime expires = DateTime.UtcNow.AddMinutes(30);

        // Save token to database
        user.SetPasswordResetToken(token, expires);
        await _context.SaveChangesAsync(ct);

        // Construct frontend reset link
        string? clientUrl = _config["ClientSettings:BaseUrl"] ?? "https://codeclash-front-three.vercel.app";
        string resetLink = $"{clientUrl}/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";

        // Send email
        await _emailService.SendPasswordResetAsync(user.Email, user.FullName, resetLink, ct);

        return Result<string>.Success(null, "If your email is registered, you will receive a reset password link shortly.");
    }
}
