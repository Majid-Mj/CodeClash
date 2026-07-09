using System;

namespace CodeClash.Domain.Entities;

public class SystemLog
{
    public Guid Id { get; private set; }
    public string Level { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Required for EF Core
    private SystemLog() { }

    public SystemLog(string level, string category, string message, string source)
    {
        Id = Guid.NewGuid();
        Level = level;
        Category = category;
        Message = message;
        Source = source;
        CreatedAt = DateTime.UtcNow;
    }
}
