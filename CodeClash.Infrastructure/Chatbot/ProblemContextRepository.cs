using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Chatbot.Dtos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CodeClash.Infrastructure.Chatbot;

public sealed class ProblemContextRepository : IProblemContextRepository
{
    private readonly string _connectionString;

    public ProblemContextRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured.");
    }

    public async Task<ProblemContext?> GetByIdAsync(Guid problemId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                p.Title,
                p.StatementMarkdown,
                p.ConstraintsJson,
                p.AllowedLanguagesJson,
                p.TimeLimitMs,
                p.MemoryLimitMb,
                CAST(p.Difficulty AS NVARCHAR(50)) AS Difficulty
            FROM Problems p
            WHERE p.Id = @ProblemId
              AND p.DeletedAt IS NULL
            """;

        await using var conn = new SqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<ProblemContext>(
            new CommandDefinition(sql, new { ProblemId = problemId }, cancellationToken: ct));
    }
}
