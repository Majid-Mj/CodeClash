using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Submissions.DTOs;
using MediatR;

namespace CodeClash.Application.Features.Submissions.Queries.GetSubmissions;

public record GetSubmissionsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null
) : IRequest<Result<PaginatedList<SubmissionSummaryDto>>>;
