using System;
using System.Collections.Generic;

namespace CodeClash.Application.Features.Submissions.DTOs;

public record SubmissionTestCaseResponseDto(Guid Id, string Status);

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
