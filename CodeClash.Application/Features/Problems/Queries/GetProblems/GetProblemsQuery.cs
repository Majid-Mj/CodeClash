using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Problems.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Problems.Queries.GetProblems;

public record GetProblemsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Difficulty = null,
    string? Category = null,
    string? Search = null,
    bool ActiveOnly = true   // Users see active only; Admin sees all
) : IRequest<Result<PaginatedList<ProblemSummaryDto>>>;