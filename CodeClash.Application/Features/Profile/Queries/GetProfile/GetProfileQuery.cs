using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Profile.DTOs;
using MediatR;
using System;

namespace CodeClash.Application.Features.Profile.Queries.GetProfile;

public record GetProfileQuery(Guid UserId) : IRequest<Result<ProfileDto>>;
