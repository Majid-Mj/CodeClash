using CodeClash.Application.Features.AIAnalysis.DTOs;
using MediatR;
using System;

namespace CodeClash.Application.Features.AIAnalysis.Commands.AnalyzeSubmission
{
    public class AnalyzeSubmissionCommand : IRequest<AIAnalysisResponseDto>
    {
        public Guid SubmissionId { get; set; }
    }
}
