namespace CodeClash.Domain.Enums;

public enum SubmissionStatus
{
    Pending = 1,
    Accepted = 2,
    WrongAnswer = 3,
    CompilationError = 4,
    RuntimeError = 5,
    TimeLimitExceeded = 6,
    MemoryLimitExceeded = 7,
    InternalError = 8
}
