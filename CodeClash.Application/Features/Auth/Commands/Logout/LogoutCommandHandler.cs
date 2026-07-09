using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    private readonly ISystemLoggingService _loggingService;

    public LogoutCommandHandler(IApplicationDbContext context, IJwtService jwtService, ISystemLoggingService loggingService)
    {
        _context = context;
        _jwtService = jwtService;
        _loggingService = loggingService;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        string hashedToken = _jwtService.HashToken(request.Dto.RefreshToken);

        if (request.Dto.AllDevices)
        {
            // Revoke ALL active refresh tokens for this user
            var allTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == request.UserId && rt.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in allTokens)
                token.Revoke();
        }
        else
        {
            // Revoke only the specific token from this session
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == hashedToken && rt.UserId == request.UserId, ct);

            if (token is null)
                return Result.Failure("Refresh token not found.");

            token.Revoke();
        }

        await _context.SaveChangesAsync(ct);
        await _loggingService.LogInfoAsync("AUTH", $"User '{request.UserId}' logged out successfully.", nameof(LogoutCommandHandler), ct);
        return Result.Success("Logged out successfully.");
    }
}