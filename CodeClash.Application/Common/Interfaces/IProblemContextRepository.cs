using CodeClash.Application.Features.Chatbot.Dtos;

namespace CodeClash.Application.Common.Interfaces;

/// <summary>
/// Dapper-backed repository for fetching structured problem data.
/// Lives in Infrastructure, declared here so Application can depend on it.
/// </summary>
public interface IProblemContextRepository
{
    Task<ProblemContext?> GetByIdAsync(Guid problemId, CancellationToken ct = default);
}
