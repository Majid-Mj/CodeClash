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
    private readonly ISystemLoggingService _loggingService;
    private readonly IBattleResolutionService _battleResolutionService;
    private readonly IDuelNotificationService _duelNotificationService;

    public CreateSubmissionCommandHandler(
        IApplicationDbContext context,
        IDockerExecutionService dockerService,
        ILogger<CreateSubmissionCommandHandler> logger,
        ISystemLoggingService loggingService,
        IBattleResolutionService battleResolutionService,
        IDuelNotificationService duelNotificationService)
    {
        _context = context;
        _dockerService = dockerService;
        _logger = logger;
        _loggingService = loggingService;
        _battleResolutionService = battleResolutionService;
        _duelNotificationService = duelNotificationService;
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
        await _loggingService.LogInfoAsync("SUBMISSION", $"Submission '{submission.Id}' created for problem '{problem.Title}' in language '{submission.Language}'. Status: Pending.", nameof(CreateSubmissionCommandHandler), ct);

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

        // 6 — Resolve wrapper template for this language
        var languageTemplate = problem.LanguageTemplates
            .FirstOrDefault(t => t.Language.Equals(language, StringComparison.OrdinalIgnoreCase));

        var wrapperTemplate = languageTemplate?.WrapperTemplate ?? string.Empty;

        if (string.IsNullOrWhiteSpace(wrapperTemplate))
        {
            _logger.LogWarning("No wrapper template found for problem {ProblemId} language {Language}. Code will be executed raw.", problem.Id, language);
        }

        // 7 — Pass to Docker Execution Service
        ExecutionResult result;
        try
        {
            result = await _dockerService.ExecuteAsync(
                submission.SourceCode,
                submission.Language,
                wrapperTemplate,
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
                testCaseDtos.Select(tc => new SubmissionTestCaseResponseDto(tc.Id, "Skipped due to Internal Error", tc.Input, tc.ExpectedOutput, null, tc.IsHidden)).ToList()
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

        if (result.Status == SubmissionStatus.Accepted)
        {
            if (dto.BattleId.HasValue)
            {
                await _battleResolutionService.ResolveBattleAsync(dto.BattleId.Value, request.UserId, dto.Language);
            }
            else
            {
                var alreadySolved = await _context.Submissions.AnyAsync(s => 
                    s.UserId == request.UserId && 
                    s.ProblemId == problem.Id && 
                    s.Status == SubmissionStatus.Accepted &&
                    s.Id != submission.Id, ct);
                    
                if (!alreadySolved)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
                    if (user != null)
                    {
                        user.AddPoints(3);
                        _logger.LogInformation("User {UserId} awarded 3 points for solving problem {ProblemId}", request.UserId, problem.Id);
                    }
                }
            }

            // ─── Custom Duel Battle Win Handling ───
            var activeDuel = await _context.CustomDuelRooms
                .FirstOrDefaultAsync(r => r.Status == "Started" && 
                                          r.SelectedProblemId == problem.Id &&
                                          (r.HostUserId == request.UserId || r.FriendUserId == request.UserId), ct);
            if (activeDuel != null)
            {
                activeDuel.Complete(request.UserId);

                var winnerUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
                Guid loserUserId = activeDuel.HostUserId == request.UserId ? activeDuel.FriendUserId : activeDuel.HostUserId;
                var loserUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == loserUserId, ct);

                if (winnerUser != null && loserUser != null)
                {
                    // Update points for winner and loser
                    winnerUser.AddPoints(15);
                    int loserPrevPoints = loserUser.TotalPoints;
                    int loserPointsChange = loserPrevPoints >= 10 ? -10 : -loserPrevPoints;
                    loserUser.AddPoints(loserPointsChange);

                    // Formatted duration
                    var elapsed = DateTime.UtcNow - activeDuel.CreatedAt;
                    string durationStr = $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}";

                    // Create BattleRecord for Winner
                    var winnerRecord = BattleRecord.Create(
                        userId: winnerUser.Id,
                        opponentName: loserUser.Username,
                        problemName: problem.Title,
                        language: submission.Language,
                        duration: durationStr,
                        score: 100,
                        isWin: true,
                        eloChange: 15
                    );

                    // Create BattleRecord for Loser
                    var loserRecord = BattleRecord.Create(
                        userId: loserUser.Id,
                        opponentName: winnerUser.Username,
                        problemName: problem.Title,
                        language: submission.Language,
                        duration: durationStr,
                        score: 0,
                        isWin: false,
                        eloChange: loserPointsChange
                    );

                    await _context.BattleRecords.AddAsync(winnerRecord, ct);
                    await _context.BattleRecords.AddAsync(loserRecord, ct);

                    _logger.LogInformation("User {UserId} awarded 15 points for winning, and loser {LoserId} adjusted by {LoserPointsChange} points in custom duel {RoomId}", request.UserId, loserUserId, loserPointsChange, activeDuel.Id);
                }

                // Send SignalR notification to the room group
                await _duelNotificationService.NotifyDuelEndedAsync(activeDuel.Id, request.UserId, ct);
            }
        }



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

        return Result<SubmissionResponseDto>.Success(response, "Submission evaluated successfully.");
    }
}
