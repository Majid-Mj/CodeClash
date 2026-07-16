using System;

namespace CodeClash.Domain.Entities;

public class BattleParticipant
{
    public Guid Id { get; private set; }
    public Guid BattleId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public int RatingBefore { get; private set; }
    public int? RatingAfter { get; private set; }
    public bool HasJoinedRoom { get; private set; }

    public void MarkJoinedRoom()
    {
        HasJoinedRoom = true;
    }

    // Navigation properties
    public Battle Battle { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private BattleParticipant() { }

    public static BattleParticipant Create(Guid battleId, Guid userId, int currentRating)
    {
        return new BattleParticipant
        {
            Id = Guid.NewGuid(),
            BattleId = battleId,
            UserId = userId,
            RatingBefore = currentRating,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void SetRatingAfter(int newRating)
    {
        RatingAfter = newRating;
    }
}
