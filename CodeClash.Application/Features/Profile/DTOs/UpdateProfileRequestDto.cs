namespace CodeClash.Application.Features.Profile.DTOs;

public record UpdateProfileRequestDto(
    string FullName,
    string? PhoneNumber,
    string Username
);
