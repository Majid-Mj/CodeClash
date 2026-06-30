namespace CodeClash.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    Guid UserId,
    string Username,
    string Email,
    string FullName,
    string Role
);