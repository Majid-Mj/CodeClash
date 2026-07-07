using System;

namespace CodeClash.Domain.Entities
{
    public class PromptHistory
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        
        public string PromptText { get; set; } = string.Empty;
        public string ResponseText { get; set; } = string.Empty;
        
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        
        public long ExecutionTimeMs { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
