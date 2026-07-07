using System;

namespace CodeClash.Domain.Entities
{
    public class AIUsageLog
    {
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        public int TokensUsed { get; set; }
        public decimal EstimatedCost { get; set; }
        
        public string Provider { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
