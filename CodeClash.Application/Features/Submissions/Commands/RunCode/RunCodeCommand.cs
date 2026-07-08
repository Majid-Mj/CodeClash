using System;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Submissions.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Submissions.Commands.RunCode;

public record RunCodeCommand(
    CreateSubmissionRequestDto Dto,
    Guid UserId
) : IRequest<Result<SubmissionResponseDto>>;
