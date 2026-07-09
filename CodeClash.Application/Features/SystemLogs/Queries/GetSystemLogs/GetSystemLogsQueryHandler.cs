using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.SystemLogs.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Features.SystemLogs.Queries.GetSystemLogs;

public class GetSystemLogsQueryHandler
    : IRequestHandler<GetSystemLogsQuery, Result<PaginatedList<SystemLogDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetSystemLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<SystemLogDto>>> Handle(
        GetSystemLogsQuery request,
        CancellationToken ct)
    {
        var query = _context.SystemLogs.AsNoTracking();

        // 1 — Filtering by Level (exact match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(request.Level))
        {
            var levelLower = request.Level.Trim().ToLower();
            query = query.Where(l => l.Level.ToLower() == levelLower);
        }

        // 2 — Filtering by Category (exact match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var categoryLower = request.Category.Trim().ToLower();
            query = query.Where(l => l.Category.ToLower() == categoryLower);
        }

        // 3 — Filtering by Date Range
        if (request.StartDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= request.EndDate.Value);
        }

        // 4 — Count total records matching criteria
        int totalCount = await query.CountAsync(ct);

        // 5 — Sort, Paginate, and Project to DTO
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new SystemLogDto(
                l.Id,
                l.Level,
                l.Category,
                l.Message,
                l.Source,
                l.CreatedAt
            ))
            .ToListAsync(ct);

        var paginatedList = new PaginatedList<SystemLogDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize
        );

        return Result<PaginatedList<SystemLogDto>>.Success(
            paginatedList,
            "System logs retrieved successfully."
        );
    }
}
