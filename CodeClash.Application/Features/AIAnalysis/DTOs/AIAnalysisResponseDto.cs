using System.Collections.Generic;

namespace CodeClash.Application.Features.AIAnalysis.DTOs
{
    public class AIAnalysisResponseDto
    {
        public string Summary { get; set; } = string.Empty;
        public string? Mistake { get; set; }
        public string? Hint { get; set; }
        public string? Optimization { get; set; }
        public string? TimeComplexity { get; set; }
        public string? SpaceComplexity { get; set; }
        
        public List<string> EdgeCases { get; set; } = new();
        
        public int CodeQualityScore { get; set; }
        public int ReadabilityScore { get; set; }
        
        public List<string> BestPractices { get; set; } = new();
        public List<string> LearningResources { get; set; } = new();
    }
}
