namespace CodeClash.Domain.Entities;

/// <summary>
/// Owned entity — lives inside the Problems table conceptually but is
/// stored in its own TestCases table. Never exposed directly to users
/// when IsHidden = true.
/// </summary>
public class TestCase
{
    public Guid Id { get; private set; }
    public Guid ProblemId { get; private set; }
    public string Input { get; private set; } = string.Empty;
    public string ExpectedOutput { get; private set; } = string.Empty;
    public bool IsHidden { get; private set; }
    public int OrderIndex { get; private set; }

    // Navigation
    public Problem Problem { get; private set; } = null!;

    private TestCase() { }

    public static TestCase Create(
        Guid problemId,
        string input,
        string expectedOutput,
        bool isHidden = false,
        int orderIndex = 0)
    {
        return new TestCase
        {
            Id = Guid.NewGuid(),
            ProblemId = problemId,
            Input = input.Trim(),
            ExpectedOutput = expectedOutput.Trim(),
            IsHidden = isHidden,
            OrderIndex = orderIndex
        };
    }

    public void Update(string input, string expectedOutput, bool isHidden)
    {
        Input = input.Trim();
        ExpectedOutput = expectedOutput.Trim();
        IsHidden = isHidden;
    }
}