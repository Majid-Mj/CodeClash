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

namespace CodeClash.Application.Features.Submissions.Commands.RunCode;

public class RunCodeCommandHandler : IRequestHandler<RunCodeCommand, Result<SubmissionResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDockerExecutionService _dockerService;
    private readonly ILogger<RunCodeCommandHandler> _logger;

    public RunCodeCommandHandler(
        IApplicationDbContext context,
        IDockerExecutionService dockerService,
        ILogger<RunCodeCommandHandler> logger)
    {
        _context = context;
        _dockerService = dockerService;
        _logger = logger;
    }

    public async Task<Result<SubmissionResponseDto>> Handle(RunCodeCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // 1 — Check if user is authenticated and exists
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, ct);
        if (!userExists)
        {
            return Result<SubmissionResponseDto>.Failure("User not found or unauthenticated.");
        }

        // 2 — Check if problem exists and load language templates
        var problem = await _context.Problems
            .Include(p => p.TestCases)
            .Include(p => p.LanguageTemplates)
            .FirstOrDefaultAsync(p => p.Id == dto.ProblemId && p.DeletedAt == null, ct);

        if (problem == null)
        {
            return Result<SubmissionResponseDto>.Failure("Problem not found.");
        }

        // 3 — Check if language is allowed
        var language = dto.Language.Trim().ToLowerInvariant();
        if (language == "c#") language = "csharp";
        if (language == "c++") language = "cpp";
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

        var isAllowed = allowedLanguages.Any(l => l.Equals(language, StringComparison.OrdinalIgnoreCase));
        if (!isAllowed && allowedLanguages.Any())
        {
            return Result<SubmissionResponseDto>.Failure($"Language '{dto.Language}' is not allowed for this problem. Allowed: {string.Join(", ", allowedLanguages)}");
        }

        // 4 — Load Test Cases
        var testCaseDtos = problem.TestCases
            .OrderBy(tc => tc.OrderIndex)
            .Select(tc => new DockerTestCaseDto(tc.Id, tc.Input, tc.ExpectedOutput, tc.IsHidden))
            .ToList();

        if (!testCaseDtos.Any())
        {
            return Result<SubmissionResponseDto>.Failure("Problem has no test cases configured.");
        }

        // 5 — Resolve wrapper template for this language
        var languageTemplate = problem.LanguageTemplates
            .FirstOrDefault(t => t.Language.Equals(language, StringComparison.OrdinalIgnoreCase));

        var wrapperTemplate = languageTemplate?.WrapperTemplate ?? string.Empty;

        if (string.IsNullOrWhiteSpace(wrapperTemplate))
        {
            _logger.LogWarning("No wrapper template found for problem {ProblemId} language {Language}. Executing raw submission.", problem.Id, language);
        }

        // 6 — Pass to Docker Execution Service
        ExecutionResult result;
        try
        {
            result = await _dockerService.ExecuteAsync(
                dto.SourceCode,
                language,
                wrapperTemplate,
                testCaseDtos,
                problem.TimeLimitMs,
                problem.MemoryLimitMb,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Docker execution failed for RunCode");
            
            var errorResponse = new SubmissionResponseDto(
                Guid.NewGuid(), // Ephemeral ID
                SubmissionStatus.InternalError.ToString(),
                0,
                testCaseDtos.Count,
                0,
                0,
                testCaseDtos.Select(tc => new SubmissionTestCaseResponseDto(tc.Id, "Skipped due to Internal Error", tc.Input, tc.ExpectedOutput, null, tc.IsHidden)).ToList()
            );
            return Result<SubmissionResponseDto>.Success(errorResponse, "Submission evaluation encountered an internal error.");
        }

        // 6 — Return Response
        var response = new SubmissionResponseDto(
            Guid.NewGuid(), // Ephemeral ID
            result.Status.ToString(),
            result.PassedCount,
            result.TotalCount,
            result.ExecutionTimeMs ?? 0,
            result.MemoryUsedBytes ?? 0,
            result.TestCases.Select(tc => {
                var originalTc = testCaseDtos.FirstOrDefault(t => t.Id == tc.Id);
                return new SubmissionTestCaseResponseDto(
                    tc.Id, 
                    tc.Status, 
                    originalTc?.Input, 
                    originalTc?.ExpectedOutput, 
                    tc.Output, 
                    originalTc?.IsHidden ?? false
                );
            }).ToList(),
            result.CompileOutput
        );

        return Result<SubmissionResponseDto>.Success(response, "Code executed successfully.");
    }
}
