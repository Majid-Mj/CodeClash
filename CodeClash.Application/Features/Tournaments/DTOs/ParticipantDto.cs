namespace CodeClash.Application.Features.Tournaments.DTOs;

public class ParticipantDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public DateTime RegisteredAt { get; set; }
}
