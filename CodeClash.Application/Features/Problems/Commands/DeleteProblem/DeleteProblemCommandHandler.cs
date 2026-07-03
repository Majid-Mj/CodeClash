using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Problems.Commands.DeleteProblem;

public class DeleteProblemCommandHandler : IRequestHandler<DeleteProblemCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteProblemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteProblemCommand request, CancellationToken ct)
    {
        var problem = await _context.Problems
            .FirstOrDefaultAsync(p => p.Id == request.ProblemId && p.DeletedAt == null, ct);

        if (problem is null)
            return Result.Failure("Problem not found.");

        // Guard: do not delete a problem assigned to an active/upcoming battle.
        // (Uncomment when Battle entity is added)
        // bool usedInActiveBattle = await _context.Battles
        //     .AnyAsync(b => b.ProblemId == problem.Id &&
        //                    (b.Status == BattleStatus.Pending ||
        //                     b.Status == BattleStatus.InProgress), ct);
        // if (usedInActiveBattle)
        //     return Result.Failure("Cannot delete a problem currently used in an active battle.");

        // Soft delete — preserves historical submission/match records
        problem.SoftDelete();

        await _context.SaveChangesAsync(ct);

        return Result.Success("Problem deleted successfully.");
    }
}