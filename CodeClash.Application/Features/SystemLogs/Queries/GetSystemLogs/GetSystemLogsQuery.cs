using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.SystemLogs.DTOs;
using MediatR;
using System;

namespace CodeClash.Application.Features.SystemLogs.Queries.GetSystemLogs;

public record GetSystemLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Level = null,
    string? Category = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<Result<PaginatedList<SystemLogDto>>>;
