using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Problems.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Problems.Commands.CreateProblem;

public record CreateProblemCommand(
    CreateProblemRequestDto Dto,
    Guid AdminUserId
) : IRequest<Result<Guid>>;