using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Auth.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Auth.Commands.Register;

public record RegisterCommand(RegisterRequestDto Dto) : IRequest<Result<string>>;