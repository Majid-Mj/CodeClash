using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Auth.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(ResetPasswordRequestDto Dto) : IRequest<Result<string>>;
