using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Profile.DTOs;
using MediatR;
using System;

namespace CodeClash.Application.Features.Profile.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, ChangePasswordRequestDto Dto) : IRequest<Result>;
