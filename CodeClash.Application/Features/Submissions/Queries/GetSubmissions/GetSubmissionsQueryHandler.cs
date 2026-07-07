using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Submissions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Submissions.Queries.GetSubmissions;

public class GetSubmissionsQueryHandler
    : IRequestHandler<GetSubmissionsQuery, Result<PaginatedList<SubmissionSummaryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetSubmissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<SubmissionSummaryDto>>> Handle(
        GetSubmissionsQuery request,
        CancellationToken ct)
    {
        var query = _context.Submissions
            .Include(s => s.User)
            .Include(s => s.Problem)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim().ToLower();
            query = query.Where(s =>
                s.User.Username.ToLower().Contains(term) ||
                s.Problem.Title.ToLower().Contains(term));
        }

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SubmissionSummaryDto(
                s.Id,
                s.User.Username,
                s.Problem.Title,
                s.Language,
                s.Status.ToString(),
                s.ExecutionTimeMs,
                s.MemoryUsedBytes,
                s.CreatedAt
            ))
            .ToListAsync(ct);

        var paginated = new PaginatedList<SubmissionSummaryDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result<PaginatedList<SubmissionSummaryDto>>.Success(
            paginated, "Submissions retrieved successfully.");
    }
}
