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
    
    public void GenerateBracket()
    {
        if (Status == TournamentStatus.Live || Status == TournamentStatus.Completed || Status == TournamentStatus.Cancelled)
            throw new InvalidOperationException($"Cannot generate bracket for tournament in status {Status}.");
            
        if (_registrations.Count < 2)
            throw new InvalidOperationException("At least 2 participants are required to generate a bracket.");

        _matches.Clear();
        
        var rand = new Random();
        var players = _registrations.OrderBy(x => rand.Next()).ToList();
        
        int n = players.Count;
        int powerOfTwo = 1;
        while (powerOfTwo < n) powerOfTwo *= 2;
        
        int byes = powerOfTwo - n;
        
        // Generate QuarterFinals or first round
        RoundType firstRound = powerOfTwo switch
        {
            2 => RoundType.Final,
            4 => RoundType.SemiFinal,
            _ => RoundType.QuarterFinal // Cap at QuarterFinals for now based on requirements, or can be dynamic
        };

        // Create first round matches
        int matchCount = powerOfTwo / 2;
        int playerIndex = 0;
        
        for (int i = 0; i < matchCount; i++)
        {
            var p1 = playerIndex < n ? players[playerIndex++].UserId : (Guid?)null;
            var p2 = (byes > 0 && p1 != null) ? null : (playerIndex < n ? players[playerIndex++].UserId : (Guid?)null);
            
            if (p2 == null && p1 != null) byes--; // Used a bye

            var match = TournamentMatch.Create(Id, firstRound, StartDate, p1, p2);
            
            // If it's a bye, auto complete
            if (p1 != null && p2 == null)
            {
                match.Finish(p1.Value);
            }
            
            _matches.Add(match);
        }
        
        // Generate subsequent rounds as placeholders
        RoundType currentRound = firstRound;
        while (currentRound != RoundType.Final)
        {
            currentRound = currentRound switch
            {
                RoundType.QuarterFinal => RoundType.SemiFinal,
                RoundType.SemiFinal => RoundType.Final,
                _ => RoundType.Final
            };
            
            matchCount /= 2;
            for (int i = 0; i < matchCount; i++)
            {
                _matches.Add(TournamentMatch.Create(Id, currentRound, StartDate));
            }
        }
        
        Status = TournamentStatus.Live;
        UpdatedAt = DateTime.UtcNow;
    }
}
