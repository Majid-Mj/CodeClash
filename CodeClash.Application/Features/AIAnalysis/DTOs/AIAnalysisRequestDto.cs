using System.ComponentModel.DataAnnotations;

namespace CodeClash.Application.Features.AIAnalysis.DTOs
{
    public class AIAnalysisRequestDto
    {
        [Required]
        public Guid SubmissionId { get; set; }
    }
}
