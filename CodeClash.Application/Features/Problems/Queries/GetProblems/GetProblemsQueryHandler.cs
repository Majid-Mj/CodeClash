using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Problems.DTOs;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Problems.Queries.GetProblems;

public class GetProblemsQueryHandler
    : IRequestHandler<GetProblemsQuery, Result<PaginatedList<ProblemSummaryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetProblemsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<ProblemSummaryDto>>> Handle(
        GetProblemsQuery request,
        CancellationToken ct)
    {
        var query = _context.Problems
            .AsNoTracking()
            .Where(p => p.DeletedAt == null);                     // never show soft-deleted

        // ── Filters ───────────────────────────────────────────────────────────
        if (request.ActiveOnly)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Difficulty) &&
            Enum.TryParse<Difficulty>(request.Difficulty, out var diff))
            query = query.Where(p => p.Difficulty == diff);

        if (!string.IsNullOrWhiteSpace(request.Category) &&
            Enum.TryParse<ProblemCategory>(request.Category, out var cat))
            query = query.Where(p => p.Category == cat);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(term) ||
                p.Slug.ToLower().Contains(term));
        }

        // ── Count before pagination (for totalCount) ──────────────────────────
        int totalCount = await query.CountAsync(ct);

        // ── Sort + paginate ───────────────────────────────────────────────────
        var problemList = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new {
                p.Id,
                p.Title,
                p.Slug,
                Difficulty = p.Difficulty.ToString(),
                Category = p.Category.ToString(),
                p.IsActive,
                p.TimeLimitMs,
                p.MemoryLimitMb
            })
            .ToListAsync(ct);

        var problemIds = problemList.Select(p => p.Id).ToList();
        
        var userStatuses = new Dictionary<Guid, string>();
        if (request.UserId.HasValue && problemIds.Any())
        {
            var submissions = await _context.Submissions
                .Where(s => s.UserId == request.UserId.Value && problemIds.Contains(s.ProblemId))
                .Select(s => new { s.ProblemId, s.Status })
                .ToListAsync(ct);

            foreach (var pId in problemIds)
            {
                var pSubs = submissions.Where(s => s.ProblemId == pId).ToList();
                if (pSubs.Any(s => s.Status == SubmissionStatus.Accepted))
                {
                    userStatuses[pId] = "Solved";
                }
                else if (pSubs.Any())
                {
                    userStatuses[pId] = "Attempted";
                }
                else
                {
                    userStatuses[pId] = "Unsolved";
                }
            }
        }

        var items = problemList.Select(p => new ProblemSummaryDto(
            p.Id,
            p.Title,
            p.Slug,
            p.Difficulty,
            p.Category,
            p.IsActive,
            p.TimeLimitMs,
            p.MemoryLimitMb,
            request.UserId.HasValue && userStatuses.ContainsKey(p.Id) ? userStatuses[p.Id] : "Unsolved"
        )).ToList();

        var paginated = new PaginatedList<ProblemSummaryDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result<PaginatedList<ProblemSummaryDto>>.Success(
            paginated, "Problems retrieved successfully.");
    }
}