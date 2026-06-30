using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CodeClash.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    // IConfiguration injected to read the frontend base URL for the verification link
    public RegisterCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IConfiguration config)
    {
        _context = context;
        _emailService = emailService;
        _config = config;
    }

    public async Task<Result<string>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // 1 — Uniqueness checks
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == dto.Email.ToLower(), ct);
        if (emailExists)
            return Result<string>.Failure("Email is already registered.");

        // Generate unique username from email prefix
        string baseUsername = dto.Email.Split('@')[0].ToLower();
        string username = baseUsername;
        int counter = 1;
        while (await _context.Users.AnyAsync(u => u.Username == username, ct))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        // 2 — Hash password
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

        // 3 — Create User entity
        var user = User.Create(dto.FullName, username, dto.Email, passwordHash, dto.PhoneNumber);

        // 4 — Persist user in database
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);

        return Result<string>.Success(
            user.Id.ToString(),
            "Account created successfully.");
    }
}