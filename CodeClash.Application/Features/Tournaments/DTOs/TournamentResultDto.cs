namespace CodeClash.Application.Features.Tournaments.DTOs;

public class TournamentResultDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    
    public int Rank { get; set; }
    public int TotalPoints { get; set; }
    public DateTime CompletedAt { get; set; }
}
