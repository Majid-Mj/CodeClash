using CodeClash.Domain.Enums;
using System;

namespace CodeClash.Domain.Entities;

public class Submission
{
    public Guid Id { get; private set; }
    public Guid ProblemId { get; private set; }
    public Guid UserId { get; private set; }
    public string Language { get; private set; } = string.Empty;
    public string SourceCode { get; private set; } = string.Empty;
    public SubmissionStatus Status { get; private set; }
    public int? ExecutionTimeMs { get; private set; }
    public long? MemoryUsedBytes { get; private set; }
    public string? CompileOutput { get; private set; }
    public string? RuntimeOutput { get; private set; }
    public int PassedCount { get; private set; }
    public int TotalCount { get; private set; }
    public string? TestCaseResultsJson { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Problem Problem { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private Submission() { }

    public static Submission Create(
        Guid problemId,
        Guid userId,
        string language,
        string sourceCode)
    {
        return new Submission
        {
            Id = Guid.NewGuid(),
            ProblemId = problemId,
            UserId = userId,
            Language = language.Trim().ToLowerInvariant(),
            SourceCode = sourceCode,
            Status = SubmissionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateResult(
        SubmissionStatus status,
        int executionTimeMs,
        long memoryUsedBytes,
        string? compileOutput,
        string? runtimeOutput,
        int passedCount,
        int totalCount,
        string testCaseResultsJson)
    {
        Status = status;
        ExecutionTimeMs = executionTimeMs;
        MemoryUsedBytes = memoryUsedBytes;
        CompileOutput = compileOutput;
        RuntimeOutput = runtimeOutput;
        PassedCount = passedCount;
        TotalCount = totalCount;
        TestCaseResultsJson = testCaseResultsJson;
    }
}
