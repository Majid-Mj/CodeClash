using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.ScheduleMatch;

public class ScheduleMatchCommandValidator : AbstractValidator<ScheduleMatchCommand>
{
    public ScheduleMatchCommandValidator()
    {
        RuleFor(x => x.TournamentId).NotEmpty();
        RuleFor(x => x.MatchId).NotEmpty();
        RuleFor(x => x.ScheduledTime).NotEmpty();
    }
}
