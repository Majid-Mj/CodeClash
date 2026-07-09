using CodeClash.Domain.Enums;

namespace CodeClash.Domain.Entities;

public class Tournament
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int MaxParticipants { get; private set; }
    public TournamentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    private readonly List<TournamentRegistration> _registrations = [];
    public IReadOnlyCollection<TournamentRegistration> Registrations => _registrations.AsReadOnly();
    
    private readonly List<TournamentMatch> _matches = [];
    public IReadOnlyCollection<TournamentMatch> Matches => _matches.AsReadOnly();
    
    private readonly List<TournamentResult> _results = [];
    public IReadOnlyCollection<TournamentResult> Results => _results.AsReadOnly();

    private Tournament() { }

    public static Tournament Create(
        string title,
        string description,
        DateTime startDate,
        DateTime endDate,
        int maxParticipants)
    {
        return new Tournament
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            MaxParticipants = maxParticipants,
            Status = TournamentStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string description,
        DateTime startDate,
        DateTime endDate,
        int maxParticipants)
    {
        Title = title.Trim();
        Description = description.Trim();
        StartDate = startDate;
        EndDate = endDate;
        MaxParticipants = maxParticipants;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        Status = TournamentStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = TournamentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void OpenRegistration()
    {
        Status = TournamentStatus.RegistrationOpen;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CloseRegistration()
    {
        Status = TournamentStatus.RegistrationClosed;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Start()
    {
        Status = TournamentStatus.Live;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = TournamentStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RegisterPlayer(Guid userId)
    {
        if (Status != TournamentStatus.RegistrationOpen)
            throw new InvalidOperationException("Registration is not open for this tournament.");

        if (_registrations.Count >= MaxParticipants)
            throw new InvalidOperationException("Tournament has reached maximum capacity.");

        if (_registrations.Any(r => r.UserId == userId))
            throw new InvalidOperationException("User is already registered.");

        _registrations.Add(TournamentRegistration.Create(Id, userId));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnregisterPlayer(Guid userId)
    {
        if (Status != TournamentStatus.RegistrationOpen)
            throw new InvalidOperationException("Registration is not open for this tournament.");

        var registration = _registrations.FirstOrDefault(r => r.UserId == userId);
        if (registration == null)
            throw new InvalidOperationException("User is not registered for this tournament.");

        _registrations.Remove(registration);
        UpdatedAt = DateTime.UtcNow;
    }
}
