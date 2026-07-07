using System;

namespace CodeClash.Application.Features.Submissions.DTOs;

public record CreateSubmissionRequestDto(
    Guid ProblemId,
    string Language,
    string SourceCode
);
