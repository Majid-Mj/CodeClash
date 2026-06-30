using CodeClash.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Net.Mail;

namespace CodeClash.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailVerificationAsync(
        string toEmail,
        string username,
        string verificationLink,
        CancellationToken ct = default)
    {
        string subject = "Verify Your CodeClash Account";
        string htmlBody = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <div style="background:#1a1a2e;padding:30px;text-align:center;">
                <h1 style="color:#e94560;margin:0;">CodeClash</h1>
                <p style="color:#ccc;margin:5px 0;">Real-Time Coding Battle Platform</p>
              </div>
              <div style="background:#16213e;padding:30px;color:#e0e0e0;">
                <h2>Welcome, {username}! 👋</h2>
                <p>Thanks for registering. Please verify your email to activate your account and start battling.</p>
                <div style="text-align:center;margin:30px 0;">
                  <a href="{verificationLink}"
                     style="background:#e94560;color:#fff;padding:14px 32px;
                            text-decoration:none;border-radius:6px;font-weight:bold;
                            font-size:16px;display:inline-block;">
                    Verify My Email
                  </a>
                </div>
                <p style="font-size:13px;color:#aaa;">
                  This link expires in <strong>24 hours</strong>. If you didn't create an account, ignore this email.
                </p>
                <p style="font-size:12px;color:#888;">
                  Or copy this link into your browser:<br>
                  <a href="{verificationLink}" style="color:#e94560;">{verificationLink}</a>
                </p>
              </div>
              <div style="background:#0f3460;padding:15px;text-align:center;color:#aaa;font-size:12px;">
                © {DateTime.UtcNow.Year} CodeClash. All rights reserved.
              </div>
            </div>
            """;

        await SendAsync(toEmail, subject, htmlBody, ct);
    }

    public async Task SendPasswordResetAsync(
        string toEmail,
        string username,
        string resetLink,
        CancellationToken ct = default)
    {
        string subject = "Reset Your CodeClash Password";
        string htmlBody = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <div style="background:#1a1a2e;padding:30px;text-align:center;">
                <h1 style="color:#e94560;margin:0;">CodeClash</h1>
              </div>
              <div style="background:#16213e;padding:30px;color:#e0e0e0;">
                <h2>Hi {username},</h2>
                <p>We received a request to reset your password. Click below to choose a new one.</p>
                <div style="text-align:center;margin:30px 0;">
                  <a href="{resetLink}"
                     style="background:#e94560;color:#fff;padding:14px 32px;
                            text-decoration:none;border-radius:6px;font-weight:bold;font-size:16px;display:inline-block;">
                    Reset My Password
                  </a>
                </div>
                <p style="font-size:13px;color:#aaa;">This link expires in <strong>30 minutes</strong>. If you did not request a reset, ignore this email — your password will not change.</p>
              </div>
            </div>
            """;

        await SendAsync(toEmail, subject, htmlBody, ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var smtp = _config.GetSection("SmtpSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            smtp["SenderName"] ?? "CodeClash",
            smtp["SenderEmail"] ?? throw new InvalidOperationException("SmtpSettings:SenderEmail is not configured.")));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new MailKit.Net.Smtp.SmtpClient();

        await client.ConnectAsync(
            smtp["Host"] ?? throw new InvalidOperationException("SmtpSettings:Host is not configured."),
            int.Parse(smtp["Port"] ?? "587"),
            SecureSocketOptions.StartTls,
            ct);

        await client.AuthenticateAsync(
            smtp["Username"] ?? throw new InvalidOperationException("SmtpSettings:Username is not configured."),
            smtp["Password"] ?? throw new InvalidOperationException("SmtpSettings:Password is not configured."),
            ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}