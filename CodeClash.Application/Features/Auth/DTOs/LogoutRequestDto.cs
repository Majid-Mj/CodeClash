namespace CodeClash.Application.Features.Auth.DTOs;

public record LogoutRequestDto(
    string RefreshToken,
    bool AllDevices = false
);