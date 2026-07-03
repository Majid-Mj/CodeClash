using FluentValidation;

namespace CodeClash.Application.Features.Problems.Queries.GetProblems;

public class GetProblemsQueryValidator : AbstractValidator<GetProblemsQuery>
{
    private static readonly string[] ValidDifficulties = ["Easy", "Medium", "Hard"];
    private static readonly string[] ValidCategories =
    [
        "Arrays", "Strings", "LinkedLists", "Trees", "Graphs",
        "DynamicProgramming", "Backtracking", "BinarySearch", "Sorting",
        "Hashing", "TwoPointers", "SlidingWindow", "Math", "Greedy", "BitManipulation"
    ];

    public GetProblemsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.Difficulty)
            .Must(d => d is null || ValidDifficulties.Contains(d))
            .WithMessage($"Difficulty must be one of: {string.Join(", ", ValidDifficulties)}.");

        RuleFor(x => x.Category)
            .Must(c => c is null || ValidCategories.Contains(c))
            .WithMessage($"Category must be one of: {string.Join(", ", ValidCategories)}.");

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.");
    }
}