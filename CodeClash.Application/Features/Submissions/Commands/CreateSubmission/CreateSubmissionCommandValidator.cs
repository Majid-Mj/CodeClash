using FluentValidation;

namespace CodeClash.Application.Features.Submissions.Commands.CreateSubmission;

public class CreateSubmissionCommandValidator : AbstractValidator<CreateSubmissionCommand>
{
    public CreateSubmissionCommandValidator()
    {
        RuleFor(x => x.Dto.ProblemId)
            .NotEmpty().WithMessage("Problem ID is required.");

        RuleFor(x => x.Dto.Language)
            .NotEmpty().WithMessage("Programming language is required.");

        RuleFor(x => x.Dto.SourceCode)
            .NotEmpty().WithMessage("Source code is required.");
    }
}
