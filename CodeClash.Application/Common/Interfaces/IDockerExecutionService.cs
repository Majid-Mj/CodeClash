using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeClash.Domain.Enums;

namespace CodeClash.Application.Common.Interfaces;

public record DockerTestCaseDto(Guid Id, string Input, string ExpectedOutput, bool IsHidden);

public record TestCaseResultDto(Guid Id, string Status, string? Output, string? Error, int ExecutionTimeMs, long MemoryUsedBytes);

public class ExecutionResult
{
    public SubmissionStatus Status { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public long? MemoryUsedBytes { get; set; }
    public string? CompileOutput { get; set; }
    public string? RuntimeOutput { get; set; }
    public int PassedCount { get; set; }
    public int TotalCount { get; set; }
    public List<TestCaseResultDto> TestCases { get; set; } = new();
}

public interface IDockerExecutionService
{
    Task<ExecutionResult> ExecuteAsync(
        string sourceCode,
        string language,
        string wrapperTemplate,
        List<DockerTestCaseDto> testCases,
        int timeLimitMs,
        int memoryLimitMb,
        CancellationToken ct);
}
