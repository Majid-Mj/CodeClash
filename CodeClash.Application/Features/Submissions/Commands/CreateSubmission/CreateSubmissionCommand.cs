using System;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Submissions.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Submissions.Commands.CreateSubmission;

public record CreateSubmissionCommand(
    CreateSubmissionRequestDto Dto,
    Guid UserId
) : IRequest<Result<SubmissionResponseDto>>;
