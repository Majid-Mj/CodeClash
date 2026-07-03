namespace CodeClash.Application.Features.Problems.DTOs;

public record CreateProblemRequestDto(
    string Title,
    string Difficulty,
    string Category,
    string StatementMarkdown,
    List<string> Constraints,
    List<string> AllowedLanguages,
    int TimeLimitMs,
    int MemoryLimitMb,
    List<TestCaseRequestDto> TestCases
);

public record TestCaseRequestDto(
    string Input,
    string ExpectedOutput,
    bool IsHidden = false
);