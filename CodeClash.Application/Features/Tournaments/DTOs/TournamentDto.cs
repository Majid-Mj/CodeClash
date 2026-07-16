namespace CodeClash.Application.Features.Tournaments.DTOs;

public class TournamentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxParticipants { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Language { get; set; }
    public int ParticipantCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
