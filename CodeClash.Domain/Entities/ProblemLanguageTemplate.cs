namespace CodeClash.Domain.Entities;

/// <summary>
/// Stores a per-language wrapper template and starter code for a Problem.
/// The WrapperTemplate contains {{submission}} which is replaced at execution
/// time with the user's submitted code — enabling the judge to compile and run
/// it without any hardcoded problem-specific logic in DockerExecutionService.
/// </summary>
public class ProblemLanguageTemplate
{
    public Guid Id { get; private set; }
    public Guid ProblemId { get; private set; }

    /// <summary>Language key (e.g. "csharp", "python", "java", "cpp", "javascript", "go", "rust").</summary>
    public string Language { get; private set; } = string.Empty;

    /// <summary>
    /// Full executable source code with {{submission}} as a placeholder for the user's code.
    /// The driver code reads from stdin (test case input) and prints to stdout.
    /// </summary>
    public string WrapperTemplate { get; private set; } = string.Empty;

    /// <summary>
    /// The skeleton/stub shown to the user in the code editor.
    /// </summary>
    public string StarterCode { get; private set; } = string.Empty;

    // Navigation
    public Problem Problem { get; private set; } = null!;

    private ProblemLanguageTemplate() { }

    public static ProblemLanguageTemplate Create(
        Guid problemId,
        string language,
        string wrapperTemplate,
        string starterCode)
    {
        return new ProblemLanguageTemplate
        {
            Id = Guid.NewGuid(),
            ProblemId = problemId,
            Language = language.ToLowerInvariant().Trim(),
            WrapperTemplate = wrapperTemplate,
            StarterCode = starterCode
        };
    }

    public void Update(string wrapperTemplate, string starterCode)
    {
        WrapperTemplate = wrapperTemplate;
        StarterCode = starterCode;
    }
}
