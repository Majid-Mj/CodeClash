using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Enums;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeClash.Infrastructure.Services;

public class DockerExecutionService : IDockerExecutionService
{
    private readonly ILogger<DockerExecutionService> _logger;
    private readonly DockerClient _dockerClient;
    private readonly ISystemLoggingService _loggingService;

    public DockerExecutionService(ILogger<DockerExecutionService> logger, IConfiguration config, ISystemLoggingService loggingService)
    {
        _logger = logger;
        _loggingService = loggingService;

        // Read custom Docker Host URI from configuration, or fallback to platform defaults
        var dockerUri = config["Docker:HostUri"];
        if (string.IsNullOrEmpty(dockerUri))
        {
            var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            dockerUri = isWindows ? "npipe://./pipe/docker_engine" : "unix:///var/run/docker.sock";
        }

        _logger.LogInformation("Initializing DockerClient with URI: {Uri}", dockerUri);
        _dockerClient = new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
    }

    public async Task<ExecutionResult> ExecuteAsync(
        string sourceCode,
        string language,
        string wrapperTemplate,
        List<DockerTestCaseDto> testCases,
        int timeLimitMs,
        int memoryLimitMb,
        CancellationToken ct)
    {
        _logger.LogInformation("Submission received. Language: {Language}, Test cases count: {Count}.", language, testCases.Count);

        var wrappedCode = WrapSourceCode(sourceCode, wrapperTemplate);

        var (imageName, fileName, compileCmd, runCmd) = GetLanguageConfiguration(language);
        await EnsureImageExistsAsync(imageName, ct);

        // 1 — Create Container
        var containerName = $"codeclash-judge-{Guid.NewGuid()}";

        // .NET SDK needs more memory than 256MB to run dotnet properly.
        // Override memory limit for csharp to at least 512MB.
        var lang = language.ToLowerInvariant().Trim();
        if ((lang == "csharp" || lang == "c#") && memoryLimitMb < 512)
            memoryLimitMb = 512;

        var containerParams = new CreateContainerParameters
        {
            Image = imageName,
            Name = containerName,
            // Keep container alive with a long-running command so we can compile/execute via Exec API
            Cmd = new List<string> { "tail", "-f", "/dev/null" },
            NetworkDisabled = true,
            HostConfig = new HostConfig
            {
                Memory = (long)memoryLimitMb * 1024 * 1024,
                MemorySwap = (long)memoryLimitMb * 1024 * 1024, // disable swap
                NanoCPUs = 1000000000, // 1 CPU Core
                AutoRemove = false // Manual cleanup to ensure we read results before deletion
            }
        };

        var container = await _dockerClient.Containers.CreateContainerAsync(containerParams, ct);
        var containerId = container.ID;

        try
        {
            // Start container
            await _dockerClient.Containers.StartContainerAsync(containerId, null, ct);
            _logger.LogInformation("Container created and started: {ContainerId}.", containerId);
            await _loggingService.LogInfoAsync("SANDBOX", $"Test execution server spun up: Sandbox Node {containerId.Substring(0, 8)}.", nameof(DockerExecutionService), ct);

            // 2 — Set up files inside /app directory
            // We run as root first to create directory and copy files
            await RunCommandDetachedAndWaitAsync(_dockerClient, containerId, new[] { "mkdir", "-p", "/app" }, ct: ct);
            await RunCommandDetachedAndWaitAsync(_dockerClient, containerId, new[] { "chmod", "-R", "777", "/app" }, ct: ct);

            var codeTarStream = TarArchiveHelper.CreateTarStream((fileName, wrappedCode));
            await _dockerClient.Containers.ExtractArchiveToContainerAsync(
                containerId,
                new ContainerPathStatParameters { Path = "/app" },
                codeTarStream,
                ct);

            // 3 — Compile Code (if compilation command is specified)
            string? compileStderr = null;
            if (compileCmd != null)
            {
                // Wrap compilation command to redirect output to files so we don't need socket hijacking
                string compileShellCmd;
                if (compileCmd.Length == 3 && compileCmd[0] == "sh" && compileCmd[1] == "-c")
                {
                    compileShellCmd = $"({compileCmd[2]}) > /app/compile_stdout.txt 2> /app/compile_stderr.txt; echo $? > /app/compile_exitcode.txt";
                }
                else
                {
                    var joinedArgs = string.Join(" ", compileCmd.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
                    compileShellCmd = $"{joinedArgs} > /app/compile_stdout.txt 2> /app/compile_stderr.txt; echo $? > /app/compile_exitcode.txt";
                }

                _logger.LogInformation("Compilation started inside container {ContainerId}.", containerId);
                await RunCommandDetachedAndWaitAsync(_dockerClient, containerId, new[] { "sh", "-c", compileShellCmd }, ct: ct);
                
                var cStdout = await ReadFileFromContainerAsync(containerId, "/app/compile_stdout.txt", ct);
                var cStderr = await ReadFileFromContainerAsync(containerId, "/app/compile_stderr.txt", ct);
                var cExitCodeStr = await ReadFileFromContainerAsync(containerId, "/app/compile_exitcode.txt", ct);
                
                int.TryParse(cExitCodeStr.Trim(), out var cExitCode);
                
                _logger.LogInformation("Compilation finished inside container {ContainerId} with exit code {ExitCode}.", containerId, cExitCode);

                if (cExitCode != 0)
                {
                    compileStderr = string.IsNullOrEmpty(cStderr) ? cStdout : cStderr;
                    await _loggingService.LogWarningAsync("SANDBOX", $"Sandbox compilation failed inside container {containerId.Substring(0, 8)}.", nameof(DockerExecutionService), ct);
                    return new ExecutionResult
                    {
                        Status = SubmissionStatus.CompilationError,
                        CompileOutput = compileStderr,
                        TotalCount = testCases.Count,
                        PassedCount = 0
                    };
                }
            }

            // Ensure non-root execution has full permissions to executable
            await RunCommandDetachedAndWaitAsync(_dockerClient, containerId, new[] { "chmod", "-R", "777", "/app" }, ct: ct);

            // 4 — Execute Test Cases sequentially
            var testCaseResults = new List<TestCaseResultDto>();
            var executionTimeMs = 0;
            var maxMemoryBytes = 0L;
            var finalStatus = SubmissionStatus.Accepted;

            for (int i = 0; i < testCases.Count; i++)
            {
                var tc = testCases[i];
                _logger.LogInformation("Test case {Index} (ID: {Id}) started.", i + 1, tc.Id);

                // Write testcase input to input.txt inside container using direct native archive extraction (100% robust for all sizes/charsets)
                var inputTarStream = TarArchiveHelper.CreateTarStream(("input.txt", tc.Input));
                await _dockerClient.Containers.ExtractArchiveToContainerAsync(
                    containerId,
                    new ContainerPathStatParameters { Path = "/app" },
                    inputTarStream,
                    ct);

                var timeoutSec = (timeLimitMs + 999) / 1000;

                // Run outer wrapper as root so we can read cgroups, but launch the target command under 'su nobody' for security/isolation
                var testCmd = new[]
                {
                    "sh", "-c",
                    $@"START_MEM=$$(cat /sys/fs/cgroup/memory.current 2>/dev/null || echo 0)
su nobody -s /bin/sh -c 'timeout -s 9 {timeoutSec}s {runCmd} < /app/input.txt' > /app/stdout.txt 2> /app/stderr.txt
EXIT_CODE=$$?
PEAK_MEM=$$(cat /sys/fs/cgroup/memory.peak 2>/dev/null || cat /sys/fs/cgroup/memory/memory.max_usage_in_bytes 2>/dev/null || echo 0)
MEM_USED=$$(($$PEAK_MEM - $$START_MEM))
if [ $$MEM_USED -lt 0 ]; then MEM_USED=0; fi
echo '===STATUS==='
echo $$EXIT_CODE
echo $$MEM_USED
echo '===STDOUT==='
cat /app/stdout.txt
echo '===STDERR==='
cat /app/stderr.txt"
                };

                var stopwatch = Stopwatch.StartNew();
                var execConfig = await _dockerClient.Exec.ExecCreateContainerAsync(
                    containerId,
                    new ContainerExecCreateParameters
                    {
                        AttachStdout = true,
                        AttachStderr = true,
                        Cmd = testCmd
                        // User is omitted (runs outer script as root to read cgroups/write wrapper files, target runs as nobody)
                    },
                    ct);

                string execStdout;
                string execStderr;
                using (var stream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(execConfig.ID, false, ct))
                {
                    var output = await stream.ReadOutputToEndAsync(ct);
                    execStdout = output.stdout;
                    execStderr = output.stderr;
                }
                stopwatch.Stop();

                var currentExecutionTime = (int)stopwatch.ElapsedMilliseconds;

                var normalized = execStdout.Replace("\r\n", "\n");
                var statusMarker = "===STATUS===\n";
                var stdoutMarker = "===STDOUT===\n";
                var stderrMarker = "===STDERR===\n";

                int statusIdx = normalized.IndexOf(statusMarker);
                int stdoutIdx = normalized.IndexOf(stdoutMarker);
                int stderrIdx = normalized.IndexOf(stderrMarker);

                int exitCode = 0;
                long peakMemoryBytes = 0L;
                string stdout = "";
                string stderr = "";

                if (statusIdx != -1 && stdoutIdx != -1 && stderrIdx != -1)
                {
                    var statusSection = normalized.Substring(statusIdx + statusMarker.Length, stdoutIdx - (statusIdx + statusMarker.Length)).Trim();
                    stdout = normalized.Substring(stdoutIdx + stdoutMarker.Length, stderrIdx - (stdoutIdx + stdoutMarker.Length));
                    stderr = normalized.Substring(stderrIdx + stderrMarker.Length);

                    var statusLines = statusSection.Split('\n');
                    if (statusLines.Length >= 2)
                    {
                        int.TryParse(statusLines[0].Trim(), out exitCode);
                        long.TryParse(statusLines[1].Trim(), out peakMemoryBytes);
                    }
                }
                else
                {
                    _logger.LogWarning("Execution output format invalid. Raw output: {Raw}", execStdout);
                    exitCode = 1;
                    stderr = string.IsNullOrEmpty(execStderr) ? execStdout : execStderr;
                }

                _logger.LogInformation("Test case {Index} finished. Time: {Time}ms, ExitCode: {ExitCode}, PeakMemory: {Memory} bytes.", 
                    i + 1, currentExecutionTime, exitCode, peakMemoryBytes);

                // Determine testcase verdict
                var tcStatus = "Passed";
                var tcVerdict = SubmissionStatus.Accepted;

                // 1. Check Time Limit Exceeded
                if (currentExecutionTime > timeLimitMs || exitCode == 124 || (exitCode == 137 && peakMemoryBytes <= (long)memoryLimitMb * 1024 * 1024))
                {
                    tcVerdict = SubmissionStatus.TimeLimitExceeded;
                    tcStatus = "Time Limit Exceeded";
                }
                // 2. Check Memory Limit Exceeded (or 137 OOM exit code)
                else if (peakMemoryBytes > (long)memoryLimitMb * 1024 * 1024 || (exitCode == 137 && peakMemoryBytes > 0))
                {
                    tcVerdict = SubmissionStatus.MemoryLimitExceeded;
                    tcStatus = "Memory Limit Exceeded";
                }
                // 3. Check Runtime Error
                else if (exitCode != 0)
                {
                    tcVerdict = SubmissionStatus.RuntimeError;
                    tcStatus = $"Runtime Error (Exit Code {exitCode})";
                }
                // 4. Check Output Correctness (Wrong Answer)
                else
                {
                    var isCorrect = CompareOutput(stdout, tc.ExpectedOutput);
                    if (!isCorrect)
                    {
                        tcVerdict = SubmissionStatus.WrongAnswer;
                        tcStatus = "Wrong Answer";
                    }
                }

                // Collect results
                executionTimeMs = Math.Max(executionTimeMs, currentExecutionTime);
                maxMemoryBytes = Math.Max(maxMemoryBytes, peakMemoryBytes);

                testCaseResults.Add(new TestCaseResultDto(
                    tc.Id,
                    tcStatus,
                    tc.IsHidden ? null : stdout.Trim(),
                    tc.IsHidden ? null : stderr.Trim(),
                    currentExecutionTime,
                    peakMemoryBytes
                ));

                if (tcVerdict != SubmissionStatus.Accepted)
                {
                    finalStatus = tcVerdict;
                    for (int j = i + 1; j < testCases.Count; j++)
                    {
                        testCaseResults.Add(new TestCaseResultDto(
                            testCases[j].Id,
                            "Skipped",
                            null,
                            null,
                            0,
                            0
                        ));
                    }
                    break;
                }
            }

            var passedCount = testCaseResults.Count(r => r.Status == "Passed");

            _logger.LogInformation("Final verdict: {Verdict}. Passed {Passed}/{Total} test cases.", finalStatus, passedCount, testCases.Count);

            if (finalStatus == SubmissionStatus.MemoryLimitExceeded)
            {
                await _loggingService.LogWarningAsync("SANDBOX", $"Memory spike detected on Sandbox Node {containerId.Substring(0, 8)} (exceeded {memoryLimitMb}MB).", nameof(DockerExecutionService), ct);
            }
            else
            {
                await _loggingService.LogInfoAsync("SANDBOX", $"Sandbox execution completed for language '{language}'. Verdict: {finalStatus}. Passed {passedCount}/{testCases.Count} test cases.", nameof(DockerExecutionService), ct);
            }

            return new ExecutionResult
            {
                Status = finalStatus,
                MemoryUsedBytes = maxMemoryBytes,
                ExecutionTimeMs = executionTimeMs,
                PassedCount = passedCount,
                TotalCount = testCases.Count,
                TestCases = testCaseResults,
                RuntimeOutput = testCaseResults.FirstOrDefault(r => !string.IsNullOrEmpty(r.Error))?.Error
            };
        }
        finally
        {
            // 5 — Clean up / Destroy container
            try
            {
                await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 1 }, ct);
                await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true }, ct);
                _logger.LogInformation("Container removed: {ContainerId}.", containerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup container {ContainerId}.", containerId);
            }
        }
    }

    private static (string ImageName, string FileName, string[]? CompileCmd, string RunCmd) GetLanguageConfiguration(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "c" => ("gcc:latest", "solution.c", new[] { "gcc", "-O3", "/app/solution.c", "-o", "/app/main", "-lm" }, "/app/main"),
            "cpp" or "c++" => ("gcc:latest", "solution.cpp", new[] { "g++", "-O3", "/app/solution.cpp", "-o", "/app/main", "-lm" }, "/app/main"),
            "java" => ("openjdk:21-slim", "Main.java", new[] { "javac", "/app/Main.java" }, "java -cp /app Main"),
            "csharp" or "c#" => (
                "mcr.microsoft.com/dotnet/sdk:8.0",
                "solution.cs",
                new[] { "sh", "-c", "dotnet new console -o /app/proj --force && cp /app/solution.cs /app/proj/Program.cs && dotnet build /app/proj -c Release -o /app/out /p:UseSharedCompilation=false" },
                "dotnet /app/out/proj.dll"
            ),
            "python" or "python3" or "py" => ("python:3.12-slim", "solution.py", null, "python3 /app/solution.py"),
            "javascript" or "js" => ("node:22-alpine", "solution.js", null, "node /app/solution.js"),
            "go" or "golang" => ("golang:alpine", "main.go", new[] { "go", "build", "-o", "/app/main", "/app/main.go" }, "/app/main"),
            "rust" => ("rust:alpine", "main.rs", new[] { "rustc", "-C", "opt-level=3", "/app/main.rs", "-o", "/app/main" }, "/app/main"),
            _ => throw new NotSupportedException($"Language '{language}' is not supported by the online judge.")
        };
    }

    private async Task EnsureImageExistsAsync(string imageName, CancellationToken ct)
    {
        try
        {
            var images = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters { All = true }, ct);
            var exists = images.Any(img => img.RepoTags != null && img.RepoTags.Any(tag => tag.Contains(imageName)));
            if (exists)
            {
                return;
            }

            _logger.LogInformation("Docker image {Image} not found locally. Starting image pull...", imageName);

            var parts = imageName.Split(':');
            var name = parts[0];
            var tag = parts.Length > 1 ? parts[1] : "latest";

            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = name, Tag = tag },
                null,
                new Progress<JSONMessage>(msg =>
                {
                    if (!string.IsNullOrEmpty(msg.Status))
                    {
                        _logger.LogDebug("Pulling {Image}: {Status}", imageName, msg.Status);
                    }
                }),
                ct);

            _logger.LogInformation("Docker image {Image} successfully pulled.", imageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking/pulling Docker image {Image}.", imageName);
            throw;
        }
    }

    private static async Task<int> RunCommandDetachedAndWaitAsync(
        DockerClient client,
        string containerId,
        string[] cmd,
        string? user = null,
        CancellationToken ct = default)
    {
        var execConfig = await client.Exec.ExecCreateContainerAsync(
            containerId,
            new ContainerExecCreateParameters
            {
                AttachStdout = false,
                AttachStderr = false,
                Cmd = cmd,
                User = user
            },
            ct);

        await client.Exec.StartContainerExecAsync(execConfig.ID, ct);

        while (true)
        {
            var inspect = await client.Exec.InspectContainerExecAsync(execConfig.ID, ct);
            if (!inspect.Running)
            {
                return (int)inspect.ExitCode;
            }
            await Task.Delay(50, ct);
        }
    }

    private async Task<string> ReadFileFromContainerAsync(string containerId, string path, CancellationToken ct)
    {
        try
        {
            var response = await _dockerClient.Containers.GetArchiveFromContainerAsync(
                containerId,
                new GetArchiveFromContainerParameters { Path = path },
                false,
                ct);

            using (var stream = response.Stream)
            {
                return await ExtractSingleFileFromTarStreamAsync(stream, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read file {Path} from container {ContainerId}", path, containerId);
            return string.Empty;
        }
    }

    private static async Task<string> ExtractSingleFileFromTarStreamAsync(Stream tarStream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await tarStream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (bytes.Length < 512) return string.Empty;

        // TAR header file size is at offset 124, length 12 (octal representation)
        var sizeString = Encoding.UTF8.GetString(bytes, 124, 12).Trim('\0', ' ');
        try
        {
            var fileSize = Convert.ToInt64(sizeString.Trim(), 8);
            if (fileSize <= 0 || bytes.Length < 512 + fileSize)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(bytes, 512, (int)fileSize);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool CompareOutput(string actual, string expected)
    {
        var actualLines = actual.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                                .Select(line => line.TrimEnd())
                                .Where(line => !string.IsNullOrEmpty(line))
                                .ToList();
                                
        var expectedLines = expected.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                                    .Select(line => line.TrimEnd())
                                    .Where(line => !string.IsNullOrEmpty(line))
                                    .ToList();
                                    
        if (actualLines.Count != expectedLines.Count)
            return false;
            
        for (int i = 0; i < actualLines.Count; i++)
        {
            if (actualLines[i] != expectedLines[i])
                return false;
        }
        
        return true;
    }

    private string WrapSourceCode(string sourceCode, string wrapperTemplate)
    {
        // Unescape source code if it contains double-escaped newlines (JSON submission case)
        if (sourceCode.Contains("\\n") && !sourceCode.Contains("\n"))
        {
            sourceCode = sourceCode
                .Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        // If no template is provided, execute source code as-is (full-program submission)
        if (string.IsNullOrWhiteSpace(wrapperTemplate))
        {
            return sourceCode;
        }

        // Inject user's submission into the driver template at the {{submission}} placeholder
        return wrapperTemplate.Replace("{{submission}}", sourceCode);
    }
}
