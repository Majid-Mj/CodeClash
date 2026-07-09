using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeClash.Application.Features.Problems.Commands.UpdateProblem;

public class UpdateProblemCommandHandler : IRequestHandler<UpdateProblemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISystemLoggingService _loggingService;

    public UpdateProblemCommandHandler(IApplicationDbContext context, ISystemLoggingService loggingService)
    {
        _context = context;
        _loggingService = loggingService;
    }

    public async Task<Result<Guid>> Handle(UpdateProblemCommand request, CancellationToken ct)
    {
        // 1 — Load with test cases (needed for ReplaceTestCases to track deletions)
        var problem = await _context.Problems
            .Include(p => p.TestCases)
            .FirstOrDefaultAsync(p => p.Id == request.ProblemId && p.DeletedAt == null, ct);

        if (problem is null)
            return Result<Guid>.Failure("Problem not found.");

        var dto = request.Dto;

        // 2 — Check title uniqueness (exclude self)
        bool titleTaken = await _context.Problems
            .AnyAsync(p =>
                p.Title.ToLower() == dto.Title.ToLower() &&
                p.Id != problem.Id &&
                p.DeletedAt == null, ct);

        if (titleTaken)
            return Result<Guid>.Failure($"Another problem with the title '{dto.Title}' already exists.");

        // 3 — Parse enums
        var difficulty = Enum.Parse<Difficulty>(dto.Difficulty, ignoreCase: true);
        var category = Enum.Parse<ProblemCategory>(dto.Category, ignoreCase: true);

        // 4 — Serialize list columns
        string constraintsJson = JsonSerializer.Serialize(dto.Constraints);
        string allowedLanguagesJson = JsonSerializer.Serialize(
            dto.AllowedLanguages.Select(l => l.ToLower()).ToList());

        // 5 — Update core fields via domain method
        problem.Update(
            dto.Title,
            difficulty,
            category,
            dto.StatementMarkdown,
            constraintsJson,
            allowedLanguagesJson,
            dto.TimeLimitMs,
            dto.MemoryLimitMb);

        // 6 — Replace test cases entirely (simpler than diff-and-patch for now)
        //     EF cascade delete handles removing the old rows
        _context.TestCases.RemoveRange(problem.TestCases);
        problem.ReplaceTestCases(
            dto.TestCases.Select(tc => (tc.Input, tc.ExpectedOutput, tc.IsHidden)));

        // 7 — Apply IsActive toggle if Admin explicitly sent it
        if (dto.IsActive.HasValue)
        {
            if (dto.IsActive.Value) problem.Activate();
            else problem.Deactivate();
        }

        await _context.SaveChangesAsync(ct);

        await _loggingService.LogInfoAsync("PROBLEM", $"Problem '{problem.Title}' (ID: {problem.Id}) updated successfully.", nameof(UpdateProblemCommandHandler), ct);
        return Result<Guid>.Success(problem.Id, "Problem updated successfully.");
    }
}