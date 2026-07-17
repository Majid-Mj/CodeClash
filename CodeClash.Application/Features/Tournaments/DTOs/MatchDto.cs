using CodeClash.Domain.Enums;

namespace CodeClash.Application.Features.Tournaments.DTOs;

public class MatchDto
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public RoundType Round { get; set; }
    public Guid? Player1Id { get; set; }
    public Guid? Player2Id { get; set; }
    public Guid? WinnerId { get; set; }
    public Guid? BattleId { get; set; }
    public Guid? AssignedProblemId { get; set; }
    public MatchStatus Status { get; set; }
    public DateTime ScheduledTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Language { get; set; }
}
