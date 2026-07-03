using CodeClash.Application.Features.Problems.DTOs;
using FluentValidation;

namespace CodeClash.Application.Features.Problems.Commands.CreateProblem;

public class CreateProblemCommandValidator : AbstractValidator<CreateProblemCommand>
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

    public CreateProblemCommandValidator()
    {
        RuleFor(x => x.Dto.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(5, 150).WithMessage("Title must be between 5 and 150 characters.");

        RuleFor(x => x.Dto.Difficulty)
            .NotEmpty().WithMessage("Difficulty is required.")
            .Must(d => ValidDifficulties.Contains(d))
            .WithMessage($"Difficulty must be one of: {string.Join(", ", ValidDifficulties)}.");

        RuleFor(x => x.Dto.Category)
            .NotEmpty().WithMessage("Category is required.")
            .Must(c => ValidCategories.Contains(c))
            .WithMessage($"Category must be one of: {string.Join(", ", ValidCategories)}.");

        RuleFor(x => x.Dto.StatementMarkdown)
            .NotEmpty().WithMessage("Problem statement is required.")
            .MinimumLength(20).WithMessage("Problem statement must be at least 20 characters.");

        RuleFor(x => x.Dto.Constraints)
            .NotNull().WithMessage("Constraints list is required (can be empty).");

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
            .WithMessage("At least one visible (non-hidden) sample test case is required.")
            .Must(tc => tc.Count(t => t.IsHidden) >= 1)
            .WithMessage("At least one hidden grading test case is required.");

        RuleForEach(x => x.Dto.TestCases).ChildRules(tc =>
        {
            tc.RuleFor(t => t.Input)
              .NotEmpty().WithMessage("Test case input cannot be empty.");
            tc.RuleFor(t => t.ExpectedOutput)
              .NotEmpty().WithMessage("Test case expected output cannot be empty.");
        });
    }
}