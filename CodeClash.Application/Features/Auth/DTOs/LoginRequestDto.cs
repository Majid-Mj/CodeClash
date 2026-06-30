namespace CodeClash.Application.Features.Auth.DTOs;

public record LoginRequestDto(
    string EmailOrUsername,
    string Password
);