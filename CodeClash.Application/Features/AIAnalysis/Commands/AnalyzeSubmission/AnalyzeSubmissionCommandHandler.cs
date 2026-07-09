using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.AIAnalysis.DTOs;
using CodeClash.Application.Features.AIAnalysis.Services;
using CodeClash.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Features.AIAnalysis.Commands.AnalyzeSubmission
{
    public class AnalyzeSubmissionCommandHandler : IRequestHandler<AnalyzeSubmissionCommand, AIAnalysisResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAIProvider _aiProvider;
        private readonly PromptBuilder _promptBuilder;
        private readonly JsonParser _jsonParser;

        public AnalyzeSubmissionCommandHandler(
            IApplicationDbContext context,
            IAIProvider aiProvider,
            PromptBuilder promptBuilder,
            JsonParser jsonParser)
        {
            _context = context;
            _aiProvider = aiProvider;
            _promptBuilder = promptBuilder;
            _jsonParser = jsonParser;
        }

        public async Task<AIAnalysisResponseDto> Handle(AnalyzeSubmissionCommand request, CancellationToken cancellationToken)
        {
            // 1. Load submission, problem, and user
            var submission = await _context.Submissions
                .Include(s => s.Problem)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);

            if (submission == null)
            {
                throw new Exception("Submission not found");
            }

            // Check if analysis already exists
            var existingAnalysis = await _context.AIAnalyses
                .FirstOrDefaultAsync(a => a.SubmissionId == request.SubmissionId, cancellationToken);

            if (existingAnalysis != null)
            {
                // Note: For a real app, we'd map existing entity to DTO and return.
                // For simplicity here, we can either re-analyze or return the mapped version.
                // Let's assume we re-analyze if triggered, or you can implement caching logic here.
            }

            // 2. Build Prompt
            var prompt = _promptBuilder.BuildPrompt(submission, submission.Problem);

            // 3. Call AI Provider
            var sw = Stopwatch.StartNew();
            var responseJson = await _aiProvider.AnalyzeAsync(prompt, null, cancellationToken);
            sw.Stop();

            // 4. Parse JSON Response
            var analysisDto = _jsonParser.Parse(responseJson);

            // 5. Store AI Analysis in DB
            var analysisEntity = new CodeClash.Domain.Entities.AIAnalysis
            {
                SubmissionId = submission.Id,
                Summary = analysisDto.Summary,
                Mistake = analysisDto.Mistake,
                Hint = analysisDto.Hint,
                Optimization = analysisDto.Optimization,
                TimeComplexity = analysisDto.TimeComplexity,
                SpaceComplexity = analysisDto.SpaceComplexity,
                CodeQualityScore = analysisDto.CodeQualityScore,
                ReadabilityScore = analysisDto.ReadabilityScore,
                EdgeCases = analysisDto.EdgeCases ?? new(),
                BestPractices = analysisDto.BestPractices ?? new(),
                LearningResources = analysisDto.LearningResources ?? new()
            };

            _context.AIAnalyses.Add(analysisEntity);

            // 6. Log Prompt History
            var history = new PromptHistory
            {
                SubmissionId = submission.Id,
                PromptText = prompt,
                ResponseText = responseJson,
                ProviderName = _aiProvider.ProviderName,
                ModelName = "Default", // Will be configured later
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };

            _context.PromptHistories.Add(history);

            // 7. Log AI Usage (Estimate tokens as roughly chars / 4)
            var tokenEstimate = (prompt.Length + responseJson.Length) / 4;
            var usageLog = new AIUsageLog
            {
                UserId = submission.UserId,
                TokensUsed = tokenEstimate,
                Provider = _aiProvider.ProviderName
            };

            _context.AIUsageLogs.Add(usageLog);

            await _context.SaveChangesAsync(cancellationToken);

            return analysisDto;
        }
    }
}
