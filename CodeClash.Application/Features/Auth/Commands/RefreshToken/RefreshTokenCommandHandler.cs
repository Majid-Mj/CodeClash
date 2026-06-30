using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Auth.DTOs;
using CodeClash.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CodeClash.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _config;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IConfiguration config)
    {
        _context = context;
        _jwtService = jwtService;
        _config = config;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var rawToken = request.Dto.RefreshToken;
        var hashedToken = _jwtService.HashToken(rawToken);

        // Find the active, valid refresh token
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken && rt.ExpiresAt > DateTime.UtcNow && rt.RevokedAt == null, ct);

        if (refreshToken is null)
            return Result<AuthResponseDto>.Failure("Invalid or expired refresh token.");

        var user = refreshToken.User;
        if (user is null || !user.IsActive)
            return Result<AuthResponseDto>.Failure("User account is inactive or suspended.");

        // Revoke the current refresh token
        refreshToken.Revoke();

        // Generate new tokens
        string accessToken = _jwtService.GenerateAccessToken(user);
        string newRawRefreshToken = _jwtService.GenerateRawRefreshToken();
        string newHashedRefreshToken = _jwtService.HashToken(newRawRefreshToken);

        // Persist the new refresh token
        int expiryDays = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        var newRefreshToken = CodeClash.Domain.Entities.RefreshToken.Create(newHashedRefreshToken, user.Id, expiryDays, refreshToken.DeviceInfo);
        
        await _context.RefreshTokens.AddAsync(newRefreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // Build response
        int expiryMinutes = int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"] ?? "15");
        var response = new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: newRawRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(expiryMinutes),
            User: new UserDto(
                UserId: user.Id,
                Username: user.Username,
                Email: user.Email,
                FullName: user.FullName,
                Role: user.Role.ToString()
            )
        );

        return Result<AuthResponseDto>.Success(response, "Token refreshed successfully.");
    }
}
