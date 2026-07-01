namespace CodeClash.Application.Features.Profile.DTOs;

public record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword
);
