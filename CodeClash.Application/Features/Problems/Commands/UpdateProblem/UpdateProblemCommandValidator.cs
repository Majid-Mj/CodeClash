using FluentValidation;

namespace CodeClash.Application.Features.Problems.Commands.UpdateProblem;

public class UpdateProblemCommandValidator : AbstractValidator<UpdateProblemCommand>
{
    private static readonly string[] ValidDifficulties = ["Easy", "Medium", "Hard"];
    private static readonly string[] ValidCategories =
    [
        "Arrays", "Strings", "LinkedLists", "Trees", "Graphs",
        "DynamicProgramming", "Backtracking", "BinarySearch", "Sorting",
        "Hashing", "TwoPointers", "SlidingWindow", "Math", "Greedy", "BitManipulation"
    ];
    private static readonly string[] ValidLanguages =
        ["csharp", "java", "python", "cpp", "javascript"];

    public UpdateProblemCommandValidator()
    {
        RuleFor(x => x.ProblemId)
            .NotEmpty().WithMessage("Problem ID is required.");

        RuleFor(x => x.Dto.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(5, 150).WithMessage("Title must be between 5 and 150 characters.");

        RuleFor(x => x.Dto.Difficulty)
            .NotEmpty()
            .Must(d => ValidDifficulties.Contains(d))
            .WithMessage($"Difficulty must be one of: {string.Join(", ", ValidDifficulties)}.");

        RuleFor(x => x.Dto.Category)
            .NotEmpty()
            .Must(c => ValidCategories.Contains(c))
            .WithMessage($"Category must be one of: {string.Join(", ", ValidCategories)}.");

        RuleFor(x => x.Dto.StatementMarkdown)
            .NotEmpty()
            .MinimumLength(20).WithMessage("Problem statement must be at least 20 characters.");

        RuleFor(x => x.Dto.AllowedLanguages)
            .NotEmpty().WithMessage("At least one allowed language is required.")
            .Must(langs => langs.All(l => ValidLanguages.Contains(l.ToLower())))
            .WithMessage($"All languages must be one of: {string.Join(", ", ValidLanguages)}.");

        RuleFor(x => x.Dto.TimeLimitMs)
            .InclusiveBetween(500, 10000)
            .WithMessage("Time limit must be between 500 and 10,000 milliseconds.");

        RuleFor(x => x.Dto.MemoryLimitMb)
            .InclusiveBetween(32, 512)
            .WithMessage("Memory limit must be between 32 and 512 MB.");

        RuleFor(x => x.Dto.TestCases)
            .NotEmpty().WithMessage("At least one test case is required.")
            .Must(tc => tc.Any(t => !t.IsHidden))
            .WithMessage("At least one visible sample test case is required.")
            .Must(tc => tc.Count(t => t.IsHidden) >= 1)
            .WithMessage("At least one hidden grading test case is required.");
    }
}