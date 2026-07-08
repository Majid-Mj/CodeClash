using System;

namespace CodeClash.Domain.Entities;

public class BattleRecord
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string OpponentName { get; private set; } = string.Empty;
    public string ProblemName { get; private set; } = string.Empty;
    public string Language { get; private set; } = string.Empty;
    public string Duration { get; private set; } = string.Empty;
    public int Score { get; private set; }
    public bool IsWin { get; private set; }
    public int EloChange { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    private BattleRecord() { } // For EF Core

    public static BattleRecord Create(
        Guid userId,
        string opponentName,
        string problemName,
        string language,
        string duration,
        int score,
        bool isWin,
        int eloChange)
    {
        return new BattleRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OpponentName = opponentName,
            ProblemName = problemName,
            Language = language,
            Duration = duration,
            Score = score,
            IsWin = isWin,
            EloChange = eloChange,
            CreatedAt = DateTime.UtcNow
        };
    }
}
