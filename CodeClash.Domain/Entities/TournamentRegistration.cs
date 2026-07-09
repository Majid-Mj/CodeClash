namespace CodeClash.Domain.Entities;

public class TournamentRegistration
{
    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime RegisteredAt { get; private set; }

    public Tournament Tournament { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private TournamentRegistration() { }

    public static TournamentRegistration Create(Guid tournamentId, Guid userId)
    {
        return new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            UserId = userId,
            RegisteredAt = DateTime.UtcNow
        };
    }
}
