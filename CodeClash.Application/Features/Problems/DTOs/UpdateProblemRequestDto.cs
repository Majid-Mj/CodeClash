namespace CodeClash.Application.Features.Problems.DTOs;

public record UpdateProblemRequestDto(
    string Title,
    string Difficulty,
    string Category,
    string StatementMarkdown,
    List<string> Constraints,
    List<string> AllowedLanguages,
    int TimeLimitMs,
    int MemoryLimitMb,
    List<TestCaseRequestDto> TestCases,
    bool? IsActive = null
);