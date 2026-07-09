namespace CodeClash.Domain.Entities;

public class TournamentResult
{
    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid UserId { get; private set; }
    
    public int Rank { get; private set; }
    public int TotalPoints { get; private set; }
    public DateTime CompletedAt { get; private set; }

    public Tournament Tournament { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private TournamentResult() { }

    public static TournamentResult Create(
        Guid tournamentId,
        Guid userId,
        int rank,
        int totalPoints)
    {
        return new TournamentResult
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            UserId = userId,
            Rank = rank,
            TotalPoints = totalPoints,
            CompletedAt = DateTime.UtcNow
        };
    }
}
