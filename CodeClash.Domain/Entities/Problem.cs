using CodeClash.Domain.Enums;

namespace CodeClash.Domain.Entities;

/// <summary>
/// Problem aggregate root.
/// Encapsulates all state mutation — no public setters.
/// Test cases are managed via domain methods to maintain invariants.
/// </summary>
public class Problem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Difficulty Difficulty { get; private set; }
    public ProblemCategory Category { get; private set; }
    public string StatementMarkdown { get; private set; } = string.Empty;
    public string ConstraintsJson { get; private set; } = "[]"; // JSON array of strings
    public string AllowedLanguagesJson { get; private set; } = "[]"; // JSON array of strings
    public int TimeLimitMs { get; private set; }
    public int MemoryLimitMb { get; private set; }
    public bool IsActive { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Navigation
    private readonly List<TestCase> _testCases = [];
    public IReadOnlyCollection<TestCase> TestCases => _testCases.AsReadOnly();

    private readonly List<ProblemLanguageTemplate> _languageTemplates = [];
    public IReadOnlyCollection<ProblemLanguageTemplate> LanguageTemplates => _languageTemplates.AsReadOnly();

    private Problem() { }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Problem Create(
        string title,
        Difficulty difficulty,
        ProblemCategory category,
        string statementMarkdown,
        string constraintsJson,
        string allowedLanguagesJson,
        int timeLimitMs,
        int memoryLimitMb,
        Guid createdByUserId)
    {
        var problem = new Problem
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Slug = GenerateSlug(title),
            Difficulty = difficulty,
            Category = category,
            StatementMarkdown = statementMarkdown.Trim(),
            ConstraintsJson = constraintsJson,
            AllowedLanguagesJson = allowedLanguagesJson,
            TimeLimitMs = timeLimitMs,
            MemoryLimitMb = memoryLimitMb,
            IsActive = false, // requires Admin activation
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return problem;
    }

    // ── Domain Methods ────────────────────────────────────────────────────────

    public void Update(
        string title,
        Difficulty difficulty,
        ProblemCategory category,
        string statementMarkdown,
        string constraintsJson,
        string allowedLanguagesJson,
        int timeLimitMs,
        int memoryLimitMb)
    {
        Title = title.Trim();
        Slug = GenerateSlug(title);
        Difficulty = difficulty;
        Category = category;
        StatementMarkdown = statementMarkdown.Trim();
        ConstraintsJson = constraintsJson;
        AllowedLanguagesJson = allowedLanguagesJson;
        TimeLimitMs = timeLimitMs;
        MemoryLimitMb = memoryLimitMb;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Soft delete — preserves historical match/submission references.</summary>
    public void SoftDelete()
    {
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDeleted => DeletedAt.HasValue;

    public void AddTestCase(string input, string expectedOutput, bool isHidden)
    {
        int nextIndex = _testCases.Count;
        var tc = TestCase.Create(Id, input, expectedOutput, isHidden, nextIndex);
        _testCases.Add(tc);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddLanguageTemplate(string language, string wrapperTemplate, string starterCode)
    {
        // Remove existing template for this language (upsert semantics)
        var existing = _languageTemplates.FirstOrDefault(
            t => t.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            _languageTemplates.Remove(existing);

        var template = ProblemLanguageTemplate.Create(Id, language, wrapperTemplate, starterCode);
        _languageTemplates.Add(template);
        UpdatedAt = DateTime.UtcNow;
    }

    public ProblemLanguageTemplate? GetLanguageTemplate(string language)
        => _languageTemplates.FirstOrDefault(
            t => t.Language.Equals(language.ToLowerInvariant().Trim(), StringComparison.OrdinalIgnoreCase));

    public void ReplaceTestCases(IEnumerable<(string Input, string ExpectedOutput, bool IsHidden)> testCases)
    {
        _testCases.Clear();
        int index = 0;
        foreach (var (input, expectedOutput, isHidden) in testCases)
        {
            _testCases.Add(TestCase.Create(Id, input, expectedOutput, isHidden, index++));
        }
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string GenerateSlug(string title)
    {
        return title.Trim()
                    .ToLower()
                    .Replace(" ", "-")
                    .Replace("'", "")
                    .Replace("\"", "")
                    .Replace("/", "-");
    }
}