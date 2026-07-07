using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Submissions.DTOs;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeClash.Application.Features.Submissions.Commands.CreateSubmission;

public class CreateSubmissionCommandHandler : IRequestHandler<CreateSubmissionCommand, Result<SubmissionResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDockerExecutionService _dockerService;
    private readonly ILogger<CreateSubmissionCommandHandler> _logger;

    public CreateSubmissionCommandHandler(
        IApplicationDbContext context,
        IDockerExecutionService dockerService,
        ILogger<CreateSubmissionCommandHandler> logger)
    {
        _context = context;
        _dockerService = dockerService;
        _logger = logger;
    }

    public async Task<Result<SubmissionResponseDto>> Handle(CreateSubmissionCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // 1 — Check if user is authenticated and exists
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, ct);
        if (!userExists)
        {
            return Result<SubmissionResponseDto>.Failure("User not found or unauthenticated.");
        }

        // 2 — Check if problem exists
        var problem = await _context.Problems
            .Include(p => p.TestCases)
            .FirstOrDefaultAsync(p => p.Id == dto.ProblemId && p.DeletedAt == null, ct);

        if (problem == null)
        {
            return Result<SubmissionResponseDto>.Failure("Problem not found.");
        }

        // 3 — Check if language is allowed
        var language = dto.Language.Trim().ToLowerInvariant();
        List<string> allowedLanguages = new();
        try
        {
            allowedLanguages = JsonSerializer.Deserialize<List<string>>(problem.AllowedLanguagesJson) 
                               ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize allowed languages for problem {ProblemId}", problem.Id);
        }

        // Ensure case-insensitive match
        var isAllowed = allowedLanguages.Any(l => l.Equals(language, StringComparison.OrdinalIgnoreCase));
        if (!isAllowed && allowedLanguages.Any())
        {
            return Result<SubmissionResponseDto>.Failure($"Language '{dto.Language}' is not allowed for this problem. Allowed: {string.Join(", ", allowedLanguages)}");
        }

        // 4 — Create Pending Submission
        var submission = Submission.Create(problem.Id, request.UserId, language, dto.SourceCode);
        await _context.Submissions.AddAsync(submission, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Submission {SubmissionId} created with status Pending.", submission.Id);

        // 5 — Load Test Cases
        var testCaseDtos = problem.TestCases
            .OrderBy(tc => tc.OrderIndex)
            .Select(tc => new DockerTestCaseDto(tc.Id, tc.Input, tc.ExpectedOutput, tc.IsHidden))
            .ToList();

        if (!testCaseDtos.Any())
        {
            // If no test cases are configured, we return Internal Error or Auto-Accept.
            // Let's mark as InternalError because a problem needs test cases.
            submission.UpdateResult(
                SubmissionStatus.InternalError,
                0,
                0,
                null,
                "No test cases configured for this problem.",
                0,
                0,
                "[]");
            await _context.SaveChangesAsync(ct);
            return Result<SubmissionResponseDto>.Failure("Problem has no test cases configured.");
        }

        // 6 — Pass to Docker Execution Service
        ExecutionResult result;
        try
        {
            result = await _dockerService.ExecuteAsync(
                submission.SourceCode,
                submission.Language,
                problem.Slug,
                testCaseDtos,
                problem.TimeLimitMs,
                problem.MemoryLimitMb,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Docker execution failed for submission {SubmissionId}", submission.Id);
            
            // Mark as InternalError and update database
            submission.UpdateResult(
                SubmissionStatus.InternalError,
                0,
                0,
                null,
                $"Internal execution error: {ex.Message}",
                0,
                testCaseDtos.Count,
                "[]"
            );
            await _context.SaveChangesAsync(ct);

            var errorResponse = new SubmissionResponseDto(
                submission.Id,
                SubmissionStatus.InternalError.ToString(),
                0,
                testCaseDtos.Count,
                0,
                0,
                testCaseDtos.Select(tc => new SubmissionTestCaseResponseDto(tc.Id, "Skipped due to Internal Error")).ToList()
            );
            return Result<SubmissionResponseDto>.Success(errorResponse, "Submission evaluation encountered an internal error.");
        }

        // 7 — Update Submission Results
        var testCaseResultsJson = JsonSerializer.Serialize(result.TestCases);
        submission.UpdateResult(
            result.Status,
            result.ExecutionTimeMs ?? 0,
            result.MemoryUsedBytes ?? 0,
            result.CompileOutput,
            result.RuntimeOutput,
            result.PassedCount,
            result.TotalCount,
            testCaseResultsJson
        );

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Submission {SubmissionId} evaluated. Verdict: {Verdict}, Passed: {Passed}/{Total}.", 
            submission.Id, result.Status, result.PassedCount, result.TotalCount);

        // 8 — Return Response
        var response = new SubmissionResponseDto(
            submission.Id,
            result.Status.ToString(),
            result.PassedCount,
            result.TotalCount,
            result.ExecutionTimeMs ?? 0,
            result.MemoryUsedBytes ?? 0,
            result.TestCases.Select(tc => new SubmissionTestCaseResponseDto(tc.Id, tc.Status)).ToList(),
            result.CompileOutput
        );

        return Result<SubmissionResponseDto>.Success(response, "Submission evaluated successfully.");
    }
}
