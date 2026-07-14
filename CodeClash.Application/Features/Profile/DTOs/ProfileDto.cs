using System;

namespace CodeClash.Application.Features.Profile.DTOs;

public record ProfileDto(
    Guid UserId,
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? ProfileImageUrl,
    DateTime CreatedAt,
    string Role,
    int Rating
);
