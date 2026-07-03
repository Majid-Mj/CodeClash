using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Auth.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(ForgotPasswordRequestDto Dto) : IRequest<Result<string>>;
