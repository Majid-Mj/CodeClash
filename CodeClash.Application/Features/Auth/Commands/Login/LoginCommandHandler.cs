using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Auth.DTOs;
using CodeClash.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CodeClash.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _config;

    public LoginCommandHandler(IApplicationDbContext context, IJwtService jwtService, IConfiguration config)
    {
        _context = context;
        _jwtService = jwtService;
        _config = config;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        string identifier = dto.EmailOrUsername.Trim().ToLower();

        // 1 — Find user by email or username
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == identifier || u.Username == identifier, ct);

        if (user is null)
            return Result<AuthResponseDto>.Failure("Username or password incorrect.", "Username or password incorrect.");

        // 2 — Account guards
        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure("Your account has been suspended. Please contact support.");

        // 3 — Verify password
        bool passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
            return Result<AuthResponseDto>.Failure("Username or password incorrect.", "Username or password incorrect.");

        // 4 — Generate tokens
        string accessToken = _jwtService.GenerateAccessToken(user);
        string rawRefreshToken = _jwtService.GenerateRawRefreshToken();
        string hashedRefreshToken = _jwtService.HashToken(rawRefreshToken);

        // 5 — Persist refresh token
        int expiryDays = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        var refreshToken = CodeClash.Domain.Entities.RefreshToken.Create(hashedRefreshToken, user.Id, expiryDays, request.DeviceInfo);
        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // 6 — Build response (return raw token to client, never the hash)
        int expiryMinutes = int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"] ?? "15");
        var response = new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(expiryMinutes),
            User: new UserDto(
                UserId: user.Id,
                Username: user.Username,
                Email: user.Email,
                FullName: user.FullName,
                Role: user.Role.ToString()
            )
        );

        return Result<AuthResponseDto>.Success(response, "Login successful.");
    }
}