using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeClash.Application.Features.Problems.Commands.CreateProblem;

public class CreateProblemCommandHandler : IRequestHandler<CreateProblemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISystemLoggingService _loggingService;

    public CreateProblemCommandHandler(IApplicationDbContext context, ISystemLoggingService loggingService)
    {
        _context = context;
        _loggingService = loggingService;
    }

    public async Task<Result<Guid>> Handle(CreateProblemCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // 1 — Check for duplicate title (case-insensitive)
        bool titleExists = await _context.Problems
            .AnyAsync(p => p.Title.ToLower() == dto.Title.ToLower() && p.DeletedAt == null, ct);

        if (titleExists)
            return Result<Guid>.Failure($"A problem with the title '{dto.Title}' already exists.");

        // 2 — Parse enums (validator already confirmed they're valid strings)
        var difficulty = Enum.Parse<Difficulty>(dto.Difficulty, ignoreCase: true);
        var category = Enum.Parse<ProblemCategory>(dto.Category, ignoreCase: true);

        // 3 — Serialize list columns to JSON
        string constraintsJson = JsonSerializer.Serialize(dto.Constraints);
        string allowedLanguagesJson = JsonSerializer.Serialize(
            dto.AllowedLanguages.Select(l => l.ToLower()).ToList());

        // 4 — Create the Problem aggregate
        var problem = Problem.Create(
            dto.Title,
            difficulty,
            category,
            dto.StatementMarkdown,
            constraintsJson,
            allowedLanguagesJson,
            dto.TimeLimitMs,
            dto.MemoryLimitMb,
            request.AdminUserId);

        // 5 — Add test cases via domain method (maintains OrderIndex + invariants)
        foreach (var tc in dto.TestCases)
            problem.AddTestCase(tc.Input, tc.ExpectedOutput, tc.IsHidden);

        // 6 — Persist
        await _context.Problems.AddAsync(problem, ct);
        await _context.SaveChangesAsync(ct);

        await _loggingService.LogInfoAsync("PROBLEM", $"Problem '{problem.Title}' (ID: {problem.Id}) created successfully.", nameof(CreateProblemCommandHandler), ct);
        return Result<Guid>.Success(problem.Id, "Problem created successfully.");
    }
}