using FluentValidation;

namespace CodeClash.Application.Features.Problems.Commands.DeleteProblem;

public class DeleteProblemCommandValidator : AbstractValidator<DeleteProblemCommand>
{
    public DeleteProblemCommandValidator()
    {
        RuleFor(x => x.ProblemId)
            .NotEmpty().WithMessage("Problem ID is required.");
    }
}