using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Problems.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeClash.Application.Features.Problems.Queries.GetProblemById;

public class GetProblemByIdQueryHandler
    : IRequestHandler<GetProblemByIdQuery, Result<ProblemDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProblemByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProblemDetailDto>> Handle(
        GetProblemByIdQuery request,
        CancellationToken ct)
    {
        var problem = await _context.Problems
            .AsNoTracking()
            .Include(p => p.TestCases.OrderBy(tc => tc.OrderIndex))
            .FirstOrDefaultAsync(
                p => p.Id == request.ProblemId && p.DeletedAt == null,
                ct);

        if (problem is null)
            return Result<ProblemDetailDto>.Failure("Problem not found.");

        // Non-admin users cannot view inactive problems
        if (!problem.IsActive && !request.IsAdmin)
            return Result<ProblemDetailDto>.Failure("Problem not found.");

        // Get creator username (Dapper would be better for larger scale, but EF is fine here)
        var creatorUsername = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == problem.CreatedByUserId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync(ct) ?? "unknown";

        // Deserialize JSON columns
        var constraints = DeserializeStringList(problem.ConstraintsJson);
        var allowedLanguages = DeserializeStringList(problem.AllowedLanguagesJson);

        // Map test cases — redact hidden ones for non-Admin callers
        var testCaseDtos = problem.TestCases
            .Select(tc => new TestCaseDto(
                tc.Id,
                (request.IsAdmin || !tc.IsHidden) ? tc.Input : null,
                (request.IsAdmin || !tc.IsHidden) ? tc.ExpectedOutput : null,
                tc.IsHidden,
                tc.OrderIndex
            ))
            .ToList();

        var dto = new ProblemDetailDto(
            problem.Id,
            problem.Title,
            problem.Slug,
            problem.Difficulty.ToString(),
            problem.Category.ToString(),
            problem.StatementMarkdown,
            constraints,
            allowedLanguages,
            problem.TimeLimitMs,
            problem.MemoryLimitMb,
            problem.IsActive,
            creatorUsername,
            problem.CreatedAt,
            testCaseDtos
        );

        return Result<ProblemDetailDto>.Success(dto, "Problem retrieved successfully.");
    }

    private static List<string> DeserializeStringList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}