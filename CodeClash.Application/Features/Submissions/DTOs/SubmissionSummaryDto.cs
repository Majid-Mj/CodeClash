using System;

namespace CodeClash.Application.Features.Submissions.DTOs;

public record SubmissionSummaryDto(
    Guid Id,
    string UserName,
    string ProblemTitle,
    string Language,
    string Status,
    int? ExecutionTimeMs,
    long? MemoryUsedBytes,
    DateTime CreatedAt
);
