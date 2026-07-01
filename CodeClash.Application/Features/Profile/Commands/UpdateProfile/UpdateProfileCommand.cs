using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Profile.DTOs;
using MediatR;
using System;

namespace CodeClash.Application.Features.Profile.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, UpdateProfileRequestDto Dto) : IRequest<Result<ProfileDto>>;
