namespace CodeClash.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string username, string verificationLink, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string username, string resetLink, CancellationToken ct = default);
    Task SendPasswordResetOtpAsync(string toEmail, string username, string otp, CancellationToken ct = default);
}