namespace CodeClash.Application.Features.Problems.DTOs;

/// <summary>Lightweight DTO returned in paginated list — no test cases, no full statement.</summary>
public record ProblemSummaryDto(
    Guid ProblemId,
    string Title,
    string Slug,
    string Difficulty,
    string Category,
    bool IsActive,
    int TimeLimitMs,
    int MemoryLimitMb
);