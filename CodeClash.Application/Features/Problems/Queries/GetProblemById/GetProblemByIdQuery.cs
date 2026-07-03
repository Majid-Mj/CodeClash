using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Problems.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Problems.Queries.GetProblemById;

public record GetProblemByIdQuery(
    Guid ProblemId,
    bool IsAdmin = false  // controls whether hidden test cases are redacted
) : IRequest<Result<ProblemDetailDto>>;