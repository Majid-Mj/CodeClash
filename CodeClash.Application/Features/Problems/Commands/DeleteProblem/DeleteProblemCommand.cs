using CodeClash.Application.Common.Models;
using MediatR;

namespace CodeClash.Application.Features.Problems.Commands.DeleteProblem;

public record DeleteProblemCommand(Guid ProblemId) : IRequest<Result>;