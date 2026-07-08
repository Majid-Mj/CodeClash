using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Profile.DTOs;
using MediatR;
using System;

namespace CodeClash.Application.Features.Profile.Queries.GetProfileStats;

public record GetProfileStatsQuery(Guid UserId) : IRequest<Result<ProfileStatsDto>>;
