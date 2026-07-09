using CodeClash.Domain.Enums;

namespace CodeClash.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? GithubId { get; private set; }
    public string? ProfileImageUrl { get; private set; }
    public int TotalPoints { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string? PasswordResetToken { get; private set; }
    public DateTime? ResetTokenExpires { get; private set; }
    public string? PasswordResetOtp { get; private set; }
    public DateTime? ResetOtpExpires { get; private set; }
    public int OtpFailedAttempts { get; private set; }
    public DateTime? OtpLockedUntil { get; private set; }


    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();

    // EF constructor
    private User() { }

    public static User Create(
        string fullName,
        string username,
        string email,
        string passwordHash,
        string? phoneNumber = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Username = username.Trim().ToLower(),
            Email = email.Trim().ToLower(),
            PasswordHash = passwordHash,
            Role = UserRole.User,
            IsActive = true,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static User CreateGitHub(
        string fullName,
        string username,
        string email,
        string githubId)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Username = username.Trim().ToLower(),
            Email = email.Trim().ToLower(),
            PasswordHash = string.Empty,
            Role = UserRole.User,
            IsActive = true,
            GithubId = githubId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void PromoteToAdmin()
    {
        Role = UserRole.Admin;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(
        string fullName,
        string? phoneNumber,
        string username)
    {
        FullName = fullName.Trim();
        PhoneNumber = phoneNumber;
        Username = username.Trim().ToLower();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfileImageUrl(string? imageUrl)
    {
        ProfileImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPoints(int points)
    {
        TotalPoints += points;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LinkGitHub(string githubId)
    {
        GithubId = githubId;
        UpdatedAt = DateTime.UtcNow;
    }


    public void SetPasswordResetToken(string token, DateTime expires)
    {
        PasswordResetToken = token;
        ResetTokenExpires = expires;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        ResetTokenExpires = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordResetOtp(string otp, DateTime expires)
    {
        PasswordResetOtp = otp;
        ResetOtpExpires = expires;
        OtpFailedAttempts = 0;
        OtpLockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearPasswordResetOtp()
    {
        PasswordResetOtp = null;
        ResetOtpExpires = null;
        OtpFailedAttempts = 0;
        OtpLockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOtpLocked()
    {
        return OtpLockedUntil.HasValue && OtpLockedUntil.Value > DateTime.UtcNow;
    }

    public void IncrementOtpFailedAttempts(int maxAttempts = 5, int lockoutMinutes = 15)
    {
        OtpFailedAttempts++;
        if (OtpFailedAttempts >= maxAttempts)
        {
            OtpLockedUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            PasswordResetOtp = null;
            ResetOtpExpires = null;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetOtpFailedAttempts()
    {
        OtpFailedAttempts = 0;
        OtpLockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

}