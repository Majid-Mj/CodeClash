using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Auth.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(LogoutRequestDto Dto, Guid UserId) : IRequest<Result>;