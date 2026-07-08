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

    public DockerExecutionService(ILogger<DockerExecutionService> logger, IConfiguration config)
    {
        _logger = logger;

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
                await RunCommandDetachedAndWaitAsync(_dockerClient, containerId, testCmd, user: "65534", ct: ct);
                stopwatch.Stop();

                var currentExecutionTime = (int)stopwatch.ElapsedMilliseconds;

                // Read output, error, exit code directly from container files
                var stdout = await ReadFileFromContainerAsync(containerId, "/app/stdout.txt", ct);
                var stderr = await ReadFileFromContainerAsync(containerId, "/app/stderr.txt", ct);
                var exitCodeStr = await ReadFileFromContainerAsync(containerId, "/app/exitcode.txt", ct);

                // Read peak memory by outputting to a file first
                var memCmd = new[]
                {
                    "sh", "-c",
                    "(cat /sys/fs/cgroup/memory.peak 2>/dev/null || cat /sys/fs/cgroup/memory/memory.max_usage_in_bytes 2>/dev/null || echo 0) > /app/memory.txt"
                };
                await RunCommandDetachedAndWaitAsync(_dockerClient, containerId, memCmd, ct: ct);
                var memStr = await ReadFileFromContainerAsync(containerId, "/app/memory.txt", ct);

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

    private string WrapSourceCode(string sourceCode, string language, string slug)
    {
        slug = slug.ToLowerInvariant().Trim();
        var lang = language.ToLowerInvariant().Trim();

        if (slug == "two-sum")
        {
            if (lang == "csharp" || lang == "c#")
            {
                return sourceCode + "\n\n" + @"
using System;
using System.Text.RegularExpressions;
using System.Linq;

public class Driver {
    public static void Main() {
        string line = System.Console.ReadLine();
        if (string.IsNullOrEmpty(line)) return;
        
        var numsMatch = Regex.Match(line, @""nums\s*=\s*\[([^\]]+)\]"");
        var targetMatch = Regex.Match(line, @""target\s*=\s*(-?\d+)"");
        
        if (!numsMatch.Success || !targetMatch.Success) return;
        
        int target = int.Parse(targetMatch.Groups[1].Value);
        int[] nums = numsMatch.Groups[1].Value
                              .Split(',')
                              .Select(int.Parse)
                              .ToArray();
                              
        var res = new Solution().TwoSum(nums, target);
        System.Console.WriteLine($""[{res[0]},{res[1]}]"");
    }
}";
            }
            if (lang == "python" || lang == "python3" || lang == "py")
            {
                return sourceCode + "\n\n" + @"
import sys
import re

line = sys.stdin.read().strip()
if line:
    nums_match = re.search(r""nums\s*=\s*\[([^\]]+)\]"", line)
    target_match = re.search(r""target\s*=\s*(-?\d+)"", line)
    if nums_match and target_match:
        target = int(target_match.group(1))
        nums = [int(x) for x in nums_match.group(1).split("","")]
        res = Solution().twoSum(nums, target)
        print(f""[{res[0]},{res[1]}]"")
";
            }
            if (lang == "javascript" || lang == "js")
            {
                return sourceCode + "\n\n" + @"
const fs = require('fs');
const input = fs.readFileSync('/dev/stdin', 'utf-8').trim();
if (input) {
    const numsMatch = input.match(/nums\s*=\s*\[([^\]]+)\]/);
    const targetMatch = input.match(/target\s*=\s*(-?\d+)/);
    if (numsMatch && targetMatch) {
        const target = parseInt(targetMatch[1], 10);
        const nums = numsMatch[1].split(',').map(Number);
        const res = twoSum(nums, target);
        console.log(""["" + res[0] + "","" + res[1] + ""]"");
    }
}
";
            }
            if (lang == "java")
            {
                var modified = sourceCode.Replace("public class Solution", "class Solution");
                return modified + "\n\n" + @"
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.Scanner;

public class Main {
    public static void main(String[] args) {
        Scanner sc = new Scanner(System.in);
        if (sc.hasNextLine()) {
            String line = sc.nextLine();
            Matcher numsMatcher = Pattern.compile(""nums\\s*=\\s*\\[([^\\]]+)\\]"").matcher(line);
            Matcher targetMatcher = Pattern.compile(""target\\s*=\\s*(-?\d+)"").matcher(line);
            if (numsMatcher.find() && targetMatcher.find()) {
                int target = Integer.parseInt(targetMatcher.group(1));
                String[] parts = numsMatcher.group(1).split("","");
                int[] nums = new int[parts.length];
                for (int i = 0; i < parts.length; i++) {
                    nums[i] = Integer.parseInt(parts[i].trim());
                }
                int[] res = new Solution().twoSum(nums, target);
                System.out.println(""["" + res[0] + "","" + res[1] + ""]"");
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
#include <string>
#include <regex>
#include <sstream>

using namespace std;
int main() {
    string line;
    if (getline(cin, line)) {
        smatch nums_match;
        smatch target_match;
        regex nums_regex(""nums\\s*=\\s*\\[([^\\]]+)\\]"");
        regex target_regex(""target\\s*=\\s*(-?\d+)"");
        if (regex_search(line, nums_match, nums_regex) && regex_search(line, target_match, target_regex)) {
            int target = stoi(target_match[1].str());
            stringstream ss(nums_match[1].str());
            string temp;
            vector<int> nums;
            while (getline(ss, temp, ',')) {
                nums.push_back(stoi(temp));
            }
            Solution sol;
            vector<int> res = sol.twoSum(nums, target);
            if (res.size() >= 2) {
                cout << ""["" << res[0] << "","" << res[1] << ""]"" << endl;
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
        string s = line.Trim();
        if (s.Length >= 2 && s[0] == '""' && s[s.Length - 1] == '""') {
            s = s.Substring(1, s.Length - 2);
        }
        bool res = new Solution().IsValid(s);
        System.Console.WriteLine(res.ToString().ToLower());
    }
}";
            }
            if (lang == "python" || lang == "python3" || lang == "py")
            {
                return sourceCode + "\n\n" + @"
import sys
line = sys.stdin.read().strip()
if len(line) >= 2 and line[0] == '""' and line[-1] == '""':
    line = line[1:-1]
res = Solution().isValid(line)
print(str(res).lower())
";
            }
            if (lang == "javascript" || lang == "js")
            {
                return sourceCode + "\n\n" + @"
const fs = require('fs');
let input = fs.readFileSync('/dev/stdin', 'utf-8').trim();
if (input.length >= 2 && input[0] === '""' && input[input.length - 1] === '""') {
    input = input.substring(1, input.length - 1);
}
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
        if (s.length() >= 2 && s.charAt(0) == '""' && s.charAt(s.length() - 1) == '""') {
            s = s.substring(1, s.length() - 1);
        }
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
        if (s.length() >= 2 && s.front() == '""' && s.back() == '""') {
            s = s.substr(1, s.length() - 2);
        }
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
