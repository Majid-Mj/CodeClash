namespace CodeClash.Application.Features.Tournaments.DTOs;

public class TournamentDetailsDto
{
    public TournamentDto Tournament { get; set; } = null!;
    public List<ParticipantDto> Participants { get; set; } = new();
    public List<MatchDto> Matches { get; set; } = new();
    public List<TournamentResultDto> Results { get; set; } = new();
}
