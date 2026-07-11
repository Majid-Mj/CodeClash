using System;
using System.Collections.Generic;

namespace CodeClash.Application.Features.Submissions.DTOs;

public record SubmissionTestCaseResponseDto(
    Guid Id, 
    string Status,
    string? Input = null,
    string? ExpectedOutput = null,
    string? ActualOutput = null,
    bool IsHidden = false
);

public record SubmissionResponseDto(
    Guid SubmissionId,
    string Status,
    int Passed,
    int Total,
    int ExecutionTime,
    long Memory,
    List<SubmissionTestCaseResponseDto> TestCases,
    string? CompileOutput = null
);
