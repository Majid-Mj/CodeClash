using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using CodeClash.API.Hubs;

namespace CodeClash.API.Services;

public class BattleResolutionService : IBattleResolutionService
{
    private readonly IApplicationDbContext _context;
    private readonly IHubContext<BattleHub> _battleHubContext;

    public BattleResolutionService(
        IApplicationDbContext context,
        IHubContext<BattleHub> battleHubContext)
    {
        _context = context;
        _battleHubContext = battleHubContext;
    }

    private static int GetKFactor(int rating, int totalBattles)
    {
        if (totalBattles < 10) return 60;   // Placement phase: fast ELO calibration
        if (rating >= 2400)    return 16;   // Master tier: stable, lower fluctuation
        if (rating >= 2000)    return 24;   // Diamond tier
        return 32;                          // Standard tier
    }

    public async Task ResolveBattleAsync(Guid battleId, Guid winnerId, string language)
    {
        var dbContext = _context as DbContext;
        using var transaction = dbContext != null ? await dbContext.Database.BeginTransactionAsync() : null;

        try
        {
            var battle = await _context.Battles
                .Include(b => b.Participants)
                .FirstOrDefaultAsync(b => b.Id == battleId);

            if (battle == null || battle.Status != BattleStatus.InProgress)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return;
            }

            battle.Complete(winnerId);

            var participants = await _context.BattleParticipants
                .Where(bp => bp.BattleId == battleId)
                .ToListAsync();

            var winnerPart = participants.FirstOrDefault(p => p.UserId == winnerId);
            var loserPart = participants.FirstOrDefault(p => p.UserId != winnerId);

            int winnerDelta = 32;
            int loserDelta = -24;

            if (winnerPart != null && loserPart != null)
            {
                // Query total battles from BattleParticipants to avoid database migration
                int winnerTotalBattles = await _context.BattleParticipants.CountAsync(bp => bp.UserId == winnerPart.UserId);
                int loserTotalBattles = await _context.BattleParticipants.CountAsync(bp => bp.UserId == loserPart.UserId);

                // Standard ELO rating algorithm
                double expectedWinner = 1.0 / (1.0 + Math.Pow(10.0, (loserPart.RatingBefore - winnerPart.RatingBefore) / 400.0));
                double expectedLoser = 1.0 / (1.0 + Math.Pow(10.0, (winnerPart.RatingBefore - loserPart.RatingBefore) / 400.0));

                int kWinner = GetKFactor(winnerPart.RatingBefore, winnerTotalBattles);
                int kLoser = GetKFactor(loserPart.RatingBefore, loserTotalBattles);

                winnerDelta = (int)Math.Round(kWinner * (1.0 - expectedWinner));
                loserDelta = (int)Math.Round(kLoser * (0.0 - expectedLoser));

                winnerPart.SetRatingAfter(winnerPart.RatingBefore + winnerDelta);
                loserPart.SetRatingAfter(loserPart.RatingBefore + loserDelta);

                var winnerUser = await _context.Users.FindAsync(new object[] { winnerPart.UserId });
                var loserUser = await _context.Users.FindAsync(new object[] { loserPart.UserId });

                if (winnerUser != null)
                {
                    winnerUser.UpdateRating(winnerDelta);
                    winnerUser.AddPoints(10); // Extra match XP
                }
                if (loserUser != null)
                {
                    loserUser.UpdateRating(loserDelta);
                }

                var problem = await _context.Problems.FindAsync(new object[] { battle.ProblemId });
                var problemName = problem?.Title ?? "Coding Arena Challenge";

                // Create global historical BattleRecords for profile dashboards
                var rec1 = BattleRecord.Create(winnerPart.UserId, loserUser?.Username ?? "Opponent", problemName, language, "N/A", 100, true, winnerDelta);
                var rec2 = BattleRecord.Create(loserPart.UserId, winnerUser?.Username ?? "Opponent", problemName, language, "N/A", 20, false, loserDelta);
                _context.BattleRecords.Add(rec1);
                _context.BattleRecords.Add(rec2);
            }

            await _context.SaveChangesAsync(default);
            if (transaction != null) await transaction.CommitAsync();

            // Notify battle room group of conclusion details
            await _battleHubContext.Clients.Group(battleId.ToString()).SendAsync("BattleEnded", new
            {
                winnerId = winnerId,
                winnerDelta = winnerDelta,
                loserDelta = loserDelta,
                isSurrender = language == "N/A"
            });
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
    }
}
