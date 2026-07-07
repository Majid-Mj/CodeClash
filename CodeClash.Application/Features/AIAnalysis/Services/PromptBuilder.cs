using CodeClash.Domain.Entities;
using System.Text.Json;

namespace CodeClash.Application.Features.AIAnalysis.Services
{
    public class PromptBuilder
    {
        public string BuildPrompt(Submission submission, Problem problem)
        {
            var problemDescription = problem.StatementMarkdown;
            var sourceCode = submission.SourceCode;
            var language = submission.Language;
            var status = submission.Status.ToString();
            var memory = submission.MemoryUsedBytes;
            var time = submission.ExecutionTimeMs;
            var passedCount = submission.PassedCount;
            var totalCount = submission.TotalCount;
            var testCasesJson = submission.TestCaseResultsJson;

            return $@"
You are a Senior Software Engineer acting as an educational code reviewer.
Analyze the following submission and provide structured feedback. DO NOT provide the complete solution.
Your goal is to guide the user to the correct solution and teach them best practices.

Problem: {problem.Title}
Difficulty: {problem.Difficulty}
Language: {language}
Status: {status}
Test Cases Passed: {passedCount} / {totalCount}
Execution Time: {time}ms
Memory Used: {memory} bytes

--- Problem Description ---
{problemDescription}

--- User Code ---
{sourceCode}

--- Test Case Results (JSON) ---
{testCasesJson}

--- Instructions ---
1. Explain WHY the code failed or if it succeeded, how it can be improved.
2. Point out the specific mistake or logical error.
3. Provide a helpful hint without giving away the exact code.
4. Explain Time and Space complexity of the user's code.
5. Provide a code quality score (0-100) and readability score (0-100).
6. Return the result STRICTLY as a JSON object matching the following schema. NO MARKDOWN formatting, NO triple backticks, JUST JSON.

{{
    ""Summary"": ""A brief explanation of the result."",
    ""Mistake"": ""Specific mistake if any. Null if perfectly correct."",
    ""Hint"": ""A helpful hint to guide them."",
    ""Optimization"": ""Suggestions for optimization."",
    ""TimeComplexity"": ""e.g., O(n)"",
    ""SpaceComplexity"": ""e.g., O(1)"",
    ""EdgeCases"": [""edge case 1"", ""edge case 2""],
    ""CodeQualityScore"": 90,
    ""ReadabilityScore"": 85,
    ""BestPractices"": [""practice 1"", ""practice 2""],
    ""LearningResources"": [""resource topic 1""]
}}
";
        }
    }
}
