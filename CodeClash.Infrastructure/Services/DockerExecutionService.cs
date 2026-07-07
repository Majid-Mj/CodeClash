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
using Microsoft.Extensions.Logging;

namespace CodeClash.Infrastructure.Services;

public class DockerExecutionService : IDockerExecutionService
{
    private readonly ILogger<DockerExecutionService> _logger;
    private readonly DockerClient _dockerClient;

    public DockerExecutionService(ILogger<DockerExecutionService> logger)
    {
        _logger = logger;

        // Automatically detect platform and configure Docker daemon URI
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        var dockerUri = isWindows ? "npipe://./pipe/docker_engine" : "unix:///var/run/docker.sock";
        _dockerClient = new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
    }

    public async Task<ExecutionResult> ExecuteAsync(
        string sourceCode,
        string language,
        string problemSlug,
        List<DockerTestCaseDto> testCases,
        int timeLimitMs,
        int memoryLimitMb,
        CancellationToken ct)
    {
        _logger.LogInformation("Submission received. Language: {Language}, Problem: {Slug}, Test cases count: {Count}.", language, problemSlug, testCases.Count);

        var wrappedCode = WrapSourceCode(sourceCode, language, problemSlug);

        var (imageName, fileName, compileCmd, runCmd) = GetLanguageConfiguration(language);
        await EnsureImageExistsAsync(imageName, ct);

        // 1 — Create Container
        var containerName = $"codeclash-judge-{Guid.NewGuid()}";
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

            // 2 — Set up files inside /app directory
            // We run as root first to create directory and copy files
            await RunExecAsync(_dockerClient, containerId, new[] { "mkdir", "-p", "/app" }, ct: ct);
            await RunExecAsync(_dockerClient, containerId, new[] { "chmod", "-R", "777", "/app" }, ct: ct);

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
                _logger.LogInformation("Compilation started inside container {ContainerId}.", containerId);
                var (cStdout, cStderr, cExitCode) = await RunExecAsync(_dockerClient, containerId, compileCmd, ct: ct);
                _logger.LogInformation("Compilation finished inside container {ContainerId} with exit code {ExitCode}.", containerId, cExitCode);

                if (cExitCode != 0)
                {
                    compileStderr = string.IsNullOrEmpty(cStderr) ? cStdout : cStderr;
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
            await RunExecAsync(_dockerClient, containerId, new[] { "chmod", "-R", "777", "/app" }, ct: ct);

            // 4 — Execute Test Cases sequentially
            var testCaseResults = new List<TestCaseResultDto>();
            var executionTimeMs = 0;
            var maxMemoryBytes = 0L;
            var finalStatus = SubmissionStatus.Accepted;

            for (int i = 0; i < testCases.Count; i++)
            {
                var tc = testCases[i];
                _logger.LogInformation("Test case {Index} (ID: {Id}) started.", i + 1, tc.Id);

                // Write testcase input to input.txt inside container
                var inputTarStream = TarArchiveHelper.CreateTarStream(("input.txt", tc.Input));
                await _dockerClient.Containers.ExtractArchiveToContainerAsync(
                    containerId,
                    new ContainerPathStatParameters { Path = "/app" },
                    inputTarStream,
                    ct);

                // Run execution
                var timeoutSec = (timeLimitMs + 999) / 1000; // ceiling to seconds
                
                // sh -c execution wrapper that redirects input/output and captures exit code securely
                // Runs target code as nobody (UID 65534)
                var testCmd = new[]
                {
                    "sh", "-c",
                    $"timeout -s 9 {timeoutSec}s {runCmd} < /app/input.txt > /app/stdout.txt 2> /app/stderr.txt; echo $? > /app/exitcode.txt"
                };

                var stopwatch = Stopwatch.StartNew();
                // Execute command inside container as user 'nobody'
                await RunExecAsync(_dockerClient, containerId, testCmd, user: "65534", ct: ct);
                stopwatch.Stop();

                var currentExecutionTime = (int)stopwatch.ElapsedMilliseconds;

                // Read output, error, exit code and cgroup memory limits
                var (stdout, _, _) = await RunExecAsync(_dockerClient, containerId, new[] { "cat", "/app/stdout.txt" }, ct: ct);
                var (stderr, _, _) = await RunExecAsync(_dockerClient, containerId, new[] { "cat", "/app/stderr.txt" }, ct: ct);
                var (exitCodeStr, _, _) = await RunExecAsync(_dockerClient, containerId, new[] { "cat", "/app/exitcode.txt" }, ct: ct);
                
                // Read peak memory from cgroup peak files
                var (memStr, _, _) = await RunExecAsync(_dockerClient, containerId, new[]
                {
                    "sh", "-c",
                    "cat /sys/fs/cgroup/memory.peak 2>/dev/null || cat /sys/fs/cgroup/memory/memory.max_usage_in_bytes 2>/dev/null || echo 0"
                }, ct: ct);

                int.TryParse(exitCodeStr.Trim(), out var exitCode);
                long.TryParse(memStr.Trim(), out var peakMemoryBytes);

                _logger.LogInformation("Test case {Index} finished. Time: {Time}ms, ExitCode: {ExitCode}, PeakMemory: {Memory} bytes.", 
                    i + 1, currentExecutionTime, exitCode, peakMemoryBytes);

                // Determine testcase verdict
                var tcStatus = "Passed";
                var tcVerdict = SubmissionStatus.Accepted;

                // 1. Check Time Limit Exceeded
                if (currentExecutionTime > timeLimitMs || exitCode == 124 || exitCode == 137 && stopwatch.ElapsedMilliseconds >= timeLimitMs)
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

                // We only want hidden testcase details obscured in response if necessary,
                // but the overall judge status stops or becomes the worst verdict.
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
                    // Add remaining test cases as Skipped to maintain the full list of test cases in the response
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
                    // Stop on first failed test case to match standard competitive programming behavior
                    break;
                }
            }

            var passedCount = testCaseResults.Count(r => r.Status == "Passed");

            _logger.LogInformation("Final verdict: {Verdict}. Passed {Passed}/{Total} test cases.", finalStatus, passedCount, testCases.Count);

            return new ExecutionResult
            {
                Status = finalStatus,
                ExecutionTimeMs = executionTimeMs,
                MemoryUsedBytes = maxMemoryBytes,
                PassedCount = passedCount,
                TotalCount = testCases.Count,
                TestCases = testCaseResults
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
                new[] { "sh", "-c", "dotnet new console -o /app/proj --force && cp /app/solution.cs /app/proj/Program.cs && dotnet build /app/proj -c Release -o /app/out" },
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

    private static async Task<(string Stdout, string Stderr, int ExitCode)> RunExecAsync(
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
                AttachStdout = true,
                AttachStderr = true,
                Cmd = cmd,
                User = user
            },
            ct);

        using var stream = await client.Exec.StartAndAttachContainerExecAsync(execConfig.ID, false, ct);
        
        var streamResult = await ReadStreamAsync(stream, ct);
        string stdout = streamResult.stdout;
        string stderr = streamResult.stderr;
        
        var inspect = await client.Exec.InspectContainerExecAsync(execConfig.ID, ct);
        return (stdout, stderr, (int)inspect.ExitCode);
    }

    private static async Task<(string stdout, string stderr)> ReadStreamAsync(MultiplexedStream stream, CancellationToken ct)
    {
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();
        var buffer = new byte[4096];
        
        while (true)
        {
            var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, ct);
            if (result.EOF)
                break;
                
            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (result.Target == MultiplexedStream.TargetStream.StandardOut)
            {
                stdoutBuilder.Append(text);
            }
            else if (result.Target == MultiplexedStream.TargetStream.StandardError)
            {
                stderrBuilder.Append(text);
            }
        }
        
        return (stdoutBuilder.ToString(), stderrBuilder.ToString());
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

    private string WrapSourceCode(string sourceCode, string language, string slug)
    {
        slug = slug.ToLowerInvariant().Trim();
        var lang = language.ToLowerInvariant().Trim();

        if (slug == "two-sum")
        {
            if (lang == "csharp" || lang == "c#")
            {
                return sourceCode + "\n\n" + @"
public class Driver {
    public static void Main() {
        string targetLine = System.Console.ReadLine();
        if (string.IsNullOrEmpty(targetLine)) return;
        int target = int.Parse(targetLine.Trim());
        string numsLine = System.Console.ReadLine();
        if (string.IsNullOrEmpty(numsLine)) return;
        var parts = numsLine.Trim().Split(new[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        int[] nums = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++) nums[i] = int.Parse(parts[i]);
        var res = new Solution().TwoSum(nums, target);
        System.Console.WriteLine(string.Join("" "", res));
    }
}";
            }
            if (lang == "python" || lang == "python3" || lang == "py")
            {
                return sourceCode + "\n\n" + @"
import sys
lines = sys.stdin.read().splitlines()
if len(lines) >= 2:
    target = int(lines[0].strip())
    nums = list(map(int, lines[1].replace(',', ' ').split()))
    sol = Solution()
    res = sol.twoSum(nums, target)
    print("" "".join(map(str, res)))
";
            }
            if (lang == "javascript" || lang == "js")
            {
                return sourceCode + "\n\n" + @"
const fs = require('fs');
const input = fs.readFileSync('/dev/stdin', 'utf-8').trim().split('\n');
if (input.length >= 2) {
    const target = parseInt(input[0].trim(), 10);
    const nums = input[1].replace(/,/g, ' ').trim().split(/\s+/).map(Number);
    const res = twoSum(nums, target);
    console.log(res.join(' '));
}
";
            }
            if (lang == "java")
            {
                var modified = sourceCode.Replace("public class Solution", "class Solution");
                return modified + "\n\n" + @"
import java.util.Scanner;
public class Main {
    public static void main(String[] args) {
        Scanner sc = new Scanner(System.in);
        if (sc.hasNextInt()) {
            int target = sc.nextInt();
            sc.nextLine();
            if (sc.hasNextLine()) {
                String[] parts = sc.nextLine().trim().split(""[\\s,]+"");
                int[] nums = new int[parts.length];
                for (int i = 0; i < parts.length; i++) nums[i] = Integer.parseInt(parts[i]);
                int[] res = new Solution().twoSum(nums, target);
                System.out.println(res[0] + "" "" + res[1]);
            }
        }
    }
}";
            }
            if (lang == "cpp" || lang == "c++")
            {
                return sourceCode + "\n\n" + @"
#include <iostream>
#include <vector>
#include <sstream>
#include <string>
using namespace std;
int main() {
    int target;
    if (cin >> target) {
        string line;
        getline(cin, line);
        if (getline(cin, line)) {
            stringstream ss(line);
            int num;
            vector<int> nums;
            while (ss >> num) {
                nums.push_back(num);
                if (ss.peek() == ',' || ss.peek() == ' ') ss.ignore();
            }
            Solution sol;
            vector<int> res = sol.twoSum(nums, target);
            if (res.size() >= 2) {
                cout << res[0] << "" "" << res[1] << endl;
            }
        }
    }
    return 0;
}";
            }
        }

        if (slug == "palindrome-number")
        {
            if (lang == "csharp" || lang == "c#")
            {
                return sourceCode + "\n\n" + @"
public class Driver {
    public static void Main() {
        string line = System.Console.ReadLine();
        if (string.IsNullOrEmpty(line)) return;
        int x = int.Parse(line.Trim());
        bool res = new Solution().IsPalindrome(x);
        System.Console.WriteLine(res.ToString().ToLower());
    }
}";
            }
            if (lang == "python" || lang == "python3" || lang == "py")
            {
                return sourceCode + "\n\n" + @"
import sys
line = sys.stdin.read().strip()
if line:
    x = int(line)
    res = Solution().isPalindrome(x)
    print(str(res).lower())
";
            }
            if (lang == "javascript" || lang == "js")
            {
                return sourceCode + "\n\n" + @"
const fs = require('fs');
const input = fs.readFileSync('/dev/stdin', 'utf-8').trim();
if (input) {
    const x = parseInt(input, 10);
    const res = isPalindrome(x);
    console.log(res.toString());
}
";
            }
            if (lang == "java")
            {
                var modified = sourceCode.Replace("public class Solution", "class Solution");
                return modified + "\n\n" + @"
import java.util.Scanner;
public class Main {
    public static void main(String[] args) {
        Scanner sc = new Scanner(System.in);
        if (sc.hasNextInt()) {
            int x = sc.nextInt();
            boolean res = new Solution().isPalindrome(x);
            System.out.println(res);
        }
    }
}";
            }
            if (lang == "cpp" || lang == "c++")
            {
                return sourceCode + "\n\n" + @"
#include <iostream>
using namespace std;
int main() {
    int x;
    if (cin >> x) {
        Solution sol;
        bool res = sol.isPalindrome(x);
        cout << (res ? ""true"" : ""false"") << endl;
    }
    return 0;
}";
            }
        }

        if (slug == "valid-parentheses")
        {
            if (lang == "csharp" || lang == "c#")
            {
                return sourceCode + "\n\n" + @"
public class Driver {
    public static void Main() {
        string line = System.Console.ReadLine();
        if (line == null) return;
        bool res = new Solution().IsValid(line.Trim());
        System.Console.WriteLine(res.ToString().ToLower());
    }
}";
            }
            if (lang == "python" || lang == "python3" || lang == "py")
            {
                return sourceCode + "\n\n" + @"
import sys
line = sys.stdin.read().strip()
res = Solution().isValid(line)
print(str(res).lower())
";
            }
            if (lang == "javascript" || lang == "js")
            {
                return sourceCode + "\n\n" + @"
const fs = require('fs');
const input = fs.readFileSync('/dev/stdin', 'utf-8').trim();
const res = isValid(input);
console.log(res.toString());
";
            }
            if (lang == "java")
            {
                var modified = sourceCode.Replace("public class Solution", "class Solution");
                return modified + "\n\n" + @"
import java.util.Scanner;
public class Main {
    public static void main(String[] args) {
        Scanner sc = new Scanner(System.in);
        String s = sc.hasNextLine() ? sc.nextLine().trim() : """";
        boolean res = new Solution().isValid(s);
        System.out.println(res);
    }
}";
            }
            if (lang == "cpp" || lang == "c++")
            {
                return sourceCode + "\n\n" + @"
#include <iostream>
#include <string>
using namespace std;
int main() {
    string s;
    if (getline(cin, s)) {
        Solution sol;
        bool res = sol.isValid(s);
        cout << (res ? ""true"" : ""false"") << endl;
    }
    return 0;
}";
            }
        }

        return sourceCode;
    }
}
