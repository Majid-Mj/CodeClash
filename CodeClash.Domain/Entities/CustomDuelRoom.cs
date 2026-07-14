using System;

namespace CodeClash.Domain.Entities;

public class CustomDuelRoom
{
    public Guid Id { get; private set; }
    public string RoomCode { get; private set; } = string.Empty;
    public Guid HostUserId { get; private set; }
    public Guid FriendUserId { get; private set; }
    public string Status { get; private set; } = "Pending"; // "Pending", "Ready", "Started", "Declined"
    public bool IsHostReady { get; private set; }
    public bool IsFriendReady { get; private set; }
    public Guid? SelectedProblemId { get; private set; }
    public Guid? WinnerId { get; private set; }
    public bool HasHostLeft { get; private set; }
    public bool HasFriendLeft { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public User HostUser { get; private set; } = null!;
    public User FriendUser { get; private set; } = null!;
    public Problem? SelectedProblem { get; private set; }
    public User? Winner { get; private set; }

    private CustomDuelRoom() { }

    public static CustomDuelRoom Create(Guid hostUserId, Guid friendUserId, string roomCode)
    {
        return new CustomDuelRoom
        {
            Id = Guid.NewGuid(),
            RoomCode = roomCode.ToUpper(),
            HostUserId = hostUserId,
            FriendUserId = friendUserId,
            Status = "Pending",
            IsHostReady = false,
            IsFriendReady = false,
            HasHostLeft = false,
            HasFriendLeft = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Accept()
    {
        Status = "Ready";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Decline()
    {
        Status = "Declined";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPlayerReady(Guid userId, bool isReady)
    {
        if (userId == HostUserId)
        {
            IsHostReady = isReady;
        }
        else if (userId == FriendUserId)
        {
            IsFriendReady = isReady;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start(Guid problemId)
    {
        Status = "Started";
        SelectedProblemId = problemId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPlayerLeft(Guid userId)
    {
        if (userId == HostUserId)
        {
            HasHostLeft = true;
        }
        else if (userId == FriendUserId)
        {
            HasFriendLeft = true;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(Guid winnerId)
    {
        Status = "Completed";
        WinnerId = winnerId;
        UpdatedAt = DateTime.UtcNow;
    }
}

