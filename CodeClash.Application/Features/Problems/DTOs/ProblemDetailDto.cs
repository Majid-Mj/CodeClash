using System;
using System.Collections.Generic;

namespace CodeClash.Application.Features.Problems.DTOs;

public record ProblemLanguageTemplateDto(
    string Language,
    string StarterCode
);

/// <summary>Full problem detail returned from GetProblemById.</summary>
public record ProblemDetailDto(
    Guid ProblemId,
    string Title,
    string Slug,
    string Difficulty,
    string Category,
    string StatementMarkdown,
    List<string> Constraints,
    List<string> AllowedLanguages,
    int TimeLimitMs,
    int MemoryLimitMb,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt,
    List<TestCaseDto> TestCases,
    List<ProblemLanguageTemplateDto> LanguageTemplates
);