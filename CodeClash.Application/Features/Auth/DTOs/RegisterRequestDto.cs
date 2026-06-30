namespace CodeClash.Application.Features.Auth.DTOs;

public record RegisterRequestDto(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber = null
);