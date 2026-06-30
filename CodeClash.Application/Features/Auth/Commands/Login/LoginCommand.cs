using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Auth.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Auth.Commands.Login;

public record LoginCommand(LoginRequestDto Dto, string? DeviceInfo) : IRequest<Result<AuthResponseDto>>;