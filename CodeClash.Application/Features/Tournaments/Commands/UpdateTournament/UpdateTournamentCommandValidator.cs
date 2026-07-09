using FluentValidation;

namespace CodeClash.Application.Features.Tournaments.Commands.UpdateTournament;

public class UpdateTournamentCommandValidator : AbstractValidator<UpdateTournamentCommand>
{
    public UpdateTournamentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Tournament Id is required.");
        
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End Date must be after Start Date.");

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(1).WithMessage("Max Participants must be at least 2.");
    }
}
