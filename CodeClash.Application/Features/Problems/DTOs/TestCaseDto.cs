namespace CodeClash.Application.Features.Problems.DTOs;

/// <summary>
/// For Admin callers — includes hidden test cases with full input/output.
/// For User callers — IsHidden cases have Input/ExpectedOutput nulled out.
/// </summary>
public record TestCaseDto(
    Guid TestCaseId,
    string? Input,
    string? ExpectedOutput,
    bool IsHidden,
    int OrderIndex
);