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
    public int? MinRating { get; private set; }
    public int? MaxRating { get; private set; }
    public string? Language { get; private set; }
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
        int maxParticipants,
        int? minRating = null,
        int? maxRating = null,
        string? language = null)
    {
        return new Tournament
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            MaxParticipants = maxParticipants,
            MinRating = minRating,
            MaxRating = maxRating,
            Language = language,
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
        int maxParticipants,
        int? minRating = null,
        int? maxRating = null,
        string? language = null)
    {
        Title = title.Trim();
        Description = description.Trim();
        StartDate = startDate;
        EndDate = endDate;
        MaxParticipants = maxParticipants;
        MinRating = minRating;
        MaxRating = maxRating;
        Language = language;
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
        if (Status == TournamentStatus.Completed) return;

        Status = TournamentStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        
        CalculateResults();
    }

    private void CalculateResults()
    {
        _results.Clear();

        var participants = _registrations.Select(r => r.UserId).ToList();
        
        foreach (var userId in participants)
        {
            var userMatches = _matches.Where(m => m.Player1Id == userId || m.Player2Id == userId).ToList();
            if (!userMatches.Any())
            {
                _results.Add(TournamentResult.Create(Id, userId, 0, 0));
                continue;
            }
            
            var reachedFinal = userMatches.Any(m => m.Round == RoundType.Final);
            var reachedSemi = userMatches.Any(m => m.Round == RoundType.SemiFinal);
            var reachedQuarter = userMatches.Any(m => m.Round == RoundType.QuarterFinal);
            
            var wonFinal = userMatches.Any(m => m.Round == RoundType.Final && m.WinnerId == userId);
            
            int rank;
            int points;
            
            if (wonFinal)
            {
                rank = 1;
                points = 100;
            }
            else if (reachedFinal)
            {
                rank = 2;
                points = 50;
            }
            else if (reachedSemi)
            {
                rank = 3;
                points = 25;
            }
            else if (reachedQuarter)
            {
                rank = 4;
                points = 10;
            }
            else
            {
                rank = 5;
                points = 5; // participation points
            }
            
            _results.Add(TournamentResult.Create(Id, userId, rank, points));
        }
    }

    public void RegisterPlayer(Guid userId, int playerRating)
    {
        if (Status != TournamentStatus.RegistrationOpen)
            throw new InvalidOperationException("Registration is not open for this tournament.");

        if (_registrations.Count >= MaxParticipants)
            throw new InvalidOperationException("Tournament has reached maximum capacity.");

        if (_registrations.Any(r => r.UserId == userId))
            throw new InvalidOperationException("User is already registered.");

        if (MinRating.HasValue && playerRating < MinRating.Value)
            throw new InvalidOperationException($"Your rating ({playerRating}) is below the minimum required ({MinRating.Value}).");

        if (MaxRating.HasValue && playerRating > MaxRating.Value)
            throw new InvalidOperationException($"Your rating ({playerRating}) is above the maximum allowed ({MaxRating.Value}).");

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

        if (Status != TournamentStatus.RegistrationOpen && Status != TournamentStatus.RegistrationClosed)
            throw new InvalidOperationException("Cannot generate bracket unless registration has been opened.");
            
        if (_registrations.Count < 2)
            throw new InvalidOperationException("At least 2 participants are required to generate a bracket.");

        _matches.Clear();
        
        var sortedPlayers = _registrations
            .OrderByDescending(r => r.User != null ? r.User.Rating : 0)
            .ThenBy(r => r.RegisteredAt)
            .ToList();
        
        int n = sortedPlayers.Count;
        int powerOfTwo = 1;
        while (powerOfTwo < n) powerOfTwo *= 2;
        
        // Generate QuarterFinals or first round
        RoundType firstRound = powerOfTwo switch
        {
            2 => RoundType.Final,
            4 => RoundType.SemiFinal,
            _ => RoundType.QuarterFinal
        };

        // Generate standard seeding order of size powerOfTwo
        var seedOrder = new List<int> { 1 };
        while (seedOrder.Count < powerOfTwo)
        {
            int nextSize = seedOrder.Count * 2;
            var nextOrder = new List<int>();
            foreach (var seed in seedOrder)
            {
                nextOrder.Add(seed);
                nextOrder.Add(nextSize + 1 - seed);
            }
            seedOrder = nextOrder;
        }

        // Create first round matches
        int matchCount = powerOfTwo / 2;
        for (int i = 0; i < matchCount; i++)
        {
            int leftSeed = seedOrder[2 * i];
            int rightSeed = seedOrder[2 * i + 1];

            var p1 = leftSeed <= n ? sortedPlayers[leftSeed - 1].UserId : (Guid?)null;
            var p2 = rightSeed <= n ? sortedPlayers[rightSeed - 1].UserId : (Guid?)null;

            var match = TournamentMatch.Create(Id, firstRound, DateTime.MinValue, p1, p2);
            
            // If it's a bye, auto complete
            if (p1 != null && p2 == null)
            {
                match.Finish(p1.Value);
            }
            else if (p1 == null && p2 != null)
            {
                match.Finish(p2.Value);
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
            var offsetMins = GetRoundOffsetMinutes(currentRound, firstRound);
            var roundScheduledTime = StartDate.AddMinutes(offsetMins);
            
            for (int i = 0; i < matchCount; i++)
            {
                _matches.Add(TournamentMatch.Create(Id, currentRound, DateTime.MinValue));
            }
        }

        // Advance byes immediately to subsequent rounds
        var firstRoundMatches = _matches.Where(m => m.Round == firstRound).ToList();
        for (int i = 0; i < firstRoundMatches.Count; i++)
        {
            var match = firstRoundMatches[i];
            if (match.Status == MatchStatus.Completed && match.WinnerId.HasValue)
            {
                var nextRound = firstRound switch
                {
                    RoundType.QuarterFinal => RoundType.SemiFinal,
                    RoundType.SemiFinal => RoundType.Final,
                    _ => RoundType.Final
                };

                var nextRoundMatches = _matches.Where(m => m.Round == nextRound).ToList();
                var nextMatchIndex = i / 2;
                if (nextMatchIndex < nextRoundMatches.Count)
                {
                    var nextMatch = nextRoundMatches[nextMatchIndex];
                    if (i % 2 == 0)
                    {
                        nextMatch.UpdatePlayer1(match.WinnerId.Value);
                    }
                    else
                    {
                        nextMatch.UpdatePlayer2(match.WinnerId.Value);
                    }
                }
            }
        }
        
        Status = TournamentStatus.Live;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SubmitMatchResult(Guid matchId, Guid winnerId)
    {
        var match = _matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
            throw new KeyNotFoundException("Match not found.");

        if (match.Status == MatchStatus.Completed)
            return;

        if (match.Player1Id != winnerId && match.Player2Id != winnerId)
            throw new InvalidOperationException("Winner must be one of the match players.");

        match.Finish(winnerId);
        UpdatedAt = DateTime.UtcNow;

        if (match.Round == RoundType.Final)
        {
            Complete();
            return;
        }

        // Advance to next round
        var nextRound = match.Round switch
        {
            RoundType.QuarterFinal => RoundType.SemiFinal,
            RoundType.SemiFinal => RoundType.Final,
            _ => RoundType.Final
        };

        // Find the index of the current match within its round
        var currentRoundMatches = _matches.Where(m => m.Round == match.Round).OrderBy(m => m.Id).ToList();
        var matchIndex = currentRoundMatches.IndexOf(match);

        // Find the next round match
        var nextRoundMatches = _matches.Where(m => m.Round == nextRound).OrderBy(m => m.Id).ToList();
        var nextMatchIndex = matchIndex / 2;
        
        if (nextMatchIndex < nextRoundMatches.Count)
        {
            var nextMatch = nextRoundMatches[nextMatchIndex];
            
            // Assign to Player1 or Player2 depending on if it's even or odd
            if (matchIndex % 2 == 0)
            {
                // We need a method to set player1/2 in TournamentMatch if it doesn't exist, or just use reflection/field if not exposed.
                // Wait, TournamentMatch doesn't have a public setter or method for updating players. 
                // Let's add UpdatePlayers to TournamentMatch, but for now we can just assume we need to update it.
                nextMatch.UpdatePlayer1(winnerId);
            }
            else
            {
                nextMatch.UpdatePlayer2(winnerId);
            }
            
            // If the next match now has a Bye (one player is null, but wait, byes are resolved in round 1), 
            // actually if we want to handle byes properly, if nextMatch gets a player but the other is null, we can't auto-win yet because the other might still be playing.
        }
    }

    private static int GetRoundOffsetMinutes(RoundType round, RoundType firstRound)
    {
        int roundLevel = GetRoundLevel(round);
        int firstRoundLevel = GetRoundLevel(firstRound);
        return Math.Max(0, (roundLevel - firstRoundLevel) * 30);
    }

    private static int GetRoundLevel(RoundType round)
    {
        return round switch
        {
            RoundType.QuarterFinal => 0,
            RoundType.SemiFinal => 1,
            RoundType.Final => 2,
            _ => 0
        };
    }
}
