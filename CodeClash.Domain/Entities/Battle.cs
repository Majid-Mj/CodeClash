using System;
using System.Collections.Generic;
using CodeClash.Domain.Enums;

namespace CodeClash.Domain.Entities;

public class Battle
{
    public Guid Id { get; private set; }
    public Guid ProblemId { get; private set; }
    public Guid? WinnerId { get; private set; }
    public BattleStatus Status { get; private set; }
    public string Mode { get; private set; } = "1v1";
    public Difficulty Difficulty { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }

    // Navigation properties
    public Problem Problem { get; private set; } = null!;
    public User? Winner { get; private set; }
    public ICollection<BattleParticipant> Participants { get; private set; } = new List<BattleParticipant>();

    private Battle() { }

    public static Battle Create(Guid problemId, Difficulty difficulty, string mode = "1v1")
    {
        return new Battle
        {
            Id = Guid.NewGuid(),
            ProblemId = problemId,
            Difficulty = difficulty,
            Mode = mode,
            Status = BattleStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        Status = BattleStatus.InProgress;
        StartTime = DateTime.UtcNow;
    }

    public void Complete(Guid winnerId)
    {
        Status = BattleStatus.Completed;
        WinnerId = winnerId;
        EndTime = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = BattleStatus.Cancelled;
        EndTime = DateTime.UtcNow;
    }
}
