namespace CodeClash.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = string.Empty;   // stored as SHA-256 hash
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? DeviceInfo { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation
    public User User { get; private set; } = null!;

    private RefreshToken() { }

    public static RefreshToken Create(
        string hashedToken,
        Guid userId,
        int expiryDays = 7,
        string? deviceInfo = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = hashedToken,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt = DateTime.UtcNow,
            DeviceInfo = deviceInfo
        };
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}