using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Problems.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Problems.Commands.UpdateProblem;

public record UpdateProblemCommand(
    Guid ProblemId,
    UpdateProblemRequestDto Dto,
    Guid AdminUserId
) : IRequest<Result<Guid>>;