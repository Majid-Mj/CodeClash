using CodeClash.Domain.Enums;

namespace CodeClash.Domain.Entities;

public class TournamentMatch
{
    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid? Player1Id { get; private set; }
    public Guid? Player2Id { get; private set; }
    public Guid? WinnerId { get; private set; }
    public Guid? AssignedProblemId { get; private set; }
    public Guid? BattleId { get; private set; }
    
    public MatchStatus Status { get; private set; }
    public RoundType Round { get; private set; }
    
    public DateTime ScheduledTime { get; private set; }
    public DateTime? StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }

    public Tournament Tournament { get; private set; } = null!;
    public User? Player1 { get; private set; }
    public User? Player2 { get; private set; }
    public User? Winner { get; private set; }
    public Problem? AssignedProblem { get; private set; }
    public Battle? Battle { get; private set; }

    private TournamentMatch() { }

    public static TournamentMatch Create(
        Guid tournamentId,
        RoundType round,
        DateTime scheduledTime,
        Guid? player1Id = null,
        Guid? player2Id = null)
    {
        return new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            Player1Id = player1Id,
            Player2Id = player2Id,
            Round = round,
            ScheduledTime = scheduledTime,
            Status = MatchStatus.Scheduled
        };
    }

    public void Start(Guid problemId, Guid battleId)
    {
        AssignedProblemId = problemId;
        BattleId = battleId;
        Status = MatchStatus.InProgress;
        StartTime = DateTime.UtcNow;
    }

    public void SetScheduledTime(DateTime scheduledTime)
    {
        ScheduledTime = scheduledTime;
    }

    public void Finish(Guid winnerId)
    {
        WinnerId = winnerId;
        Status = MatchStatus.Completed;
        EndTime = DateTime.UtcNow;
    }

    public void UpdatePlayer1(Guid playerId)
    {
        Player1Id = playerId;
    }

    public void UpdatePlayer2(Guid playerId)
    {
        Player2Id = playerId;
    }
    
    public void FinishAsDraw()
    {
        Status = MatchStatus.Completed;
        EndTime = DateTime.UtcNow;
    }
}
