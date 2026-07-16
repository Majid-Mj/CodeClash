using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Tournaments.Commands.StartMatch;

public class StartMatchCommandHandler : IRequestHandler<StartMatchCommand>
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IApplicationDbContext _context;
    private readonly ITournamentNotificationService _tournamentNotificationService;

    public StartMatchCommandHandler(
        ITournamentRepository tournamentRepository,
        IApplicationDbContext context,
        ITournamentNotificationService tournamentNotificationService)
    {
        _tournamentRepository = tournamentRepository;
        _context = context;
        _tournamentNotificationService = tournamentNotificationService;
    }

    public async Task Handle(StartMatchCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(request.TournamentId, cancellationToken);
        if (tournament == null)
            throw new KeyNotFoundException($"Tournament with Id {request.TournamentId} not found.");

        var match = tournament.Matches.FirstOrDefault(m => m.Id == request.MatchId);
        if (match == null)
            throw new KeyNotFoundException($"Match with Id {request.MatchId} not found.");

        if (match.Status != MatchStatus.Scheduled)
            throw new InvalidOperationException("Match is not in scheduled status.");

        if (!match.Player1Id.HasValue || !match.Player2Id.HasValue)
            throw new InvalidOperationException("Cannot start match: Players are not fully decided yet (TBD).");

        if (match.Round == RoundType.Final)
        {
            var unfinishedSemi = tournament.Matches.Any(m => m.Round == RoundType.SemiFinal && m.Status != MatchStatus.Completed);
            if (unfinishedSemi)
                throw new InvalidOperationException("Cannot start Final: All Semi Final matches must be completed first.");
        }
        else if (match.Round == RoundType.SemiFinal)
        {
            var unfinishedQuarter = tournament.Matches.Any(m => m.Round == RoundType.QuarterFinal && m.Status != MatchStatus.Completed);
            if (unfinishedQuarter)
                throw new InvalidOperationException("Cannot start Semi Final: All Quarter Final matches must be completed first.");
        }

        // Pick a random problem
        Problem? problem = null;
        var activeProblems = await _context.Problems
            .Where(p => p.DeletedAt == null && p.IsActive)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(tournament.Language))
        {
            var reqLang = tournament.Language.Trim().ToLowerInvariant();
            if (reqLang == "c#") reqLang = "csharp";
            if (reqLang == "c++") reqLang = "cpp";

            var matchingProblems = activeProblems.Where(p => {
                try
                {
                    var allowed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.AllowedLanguagesJson);
                    return allowed != null && allowed.Any(l => l.Equals(reqLang, StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    return false;
                }
            }).ToList();

            if (matchingProblems.Any())
            {
                var random = new Random();
                problem = matchingProblems[random.Next(matchingProblems.Count)];
            }
        }

        if (problem == null)
        {
            if (!activeProblems.Any())
            {
                // Try any problem if no active ones
                var anyProblems = await _context.Problems.ToListAsync(cancellationToken);
                if (!anyProblems.Any())
                    throw new InvalidOperationException("No problems found in the database to assign.");
                
                var random = new Random();
                problem = anyProblems[random.Next(anyProblems.Count)];
            }
            else
            {
                var random = new Random();
                problem = activeProblems[random.Next(activeProblems.Count)];
            }
        }

        // Create the Battle
        var battle = Battle.Create(problem.Id, problem.Difficulty, mode: "Tournament");
        battle.Start();
        _context.Battles.Add(battle);

        // Fetch Player ELOs and create participants
        var player1Rating = 1200;
        var player2Rating = 1200;

        if (match.Player1Id.HasValue)
        {
            var p1 = await _context.Users.FirstOrDefaultAsync(u => u.Id == match.Player1Id.Value, cancellationToken);
            if (p1 != null) player1Rating = p1.Rating;

            var bp1 = BattleParticipant.Create(battle.Id, match.Player1Id.Value, player1Rating);
            _context.BattleParticipants.Add(bp1);
            
            var notif = new Notification(match.Player1Id.Value, "Tournament Match Started", $"Your match for tournament '{tournament.Title}' has started!", "info");
            _context.Notifications.Add(notif);
        }

        if (match.Player2Id.HasValue)
        {
            var p2 = await _context.Users.FirstOrDefaultAsync(u => u.Id == match.Player2Id.Value, cancellationToken);
            if (p2 != null) player2Rating = p2.Rating;

            var bp2 = BattleParticipant.Create(battle.Id, match.Player2Id.Value, player2Rating);
            _context.BattleParticipants.Add(bp2);
            
            var notif = new Notification(match.Player2Id.Value, "Tournament Match Started", $"Your match for tournament '{tournament.Title}' has started!", "info");
            _context.Notifications.Add(notif);
        }

        match.Start(problem.Id, battle.Id);

        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _tournamentNotificationService.NotifyMatchStartedAsync(
            tournament.Id,
            match.Id,
            match.Player1Id,
            match.Player2Id,
            battle.Id,
            tournament.Language);
    }
}
