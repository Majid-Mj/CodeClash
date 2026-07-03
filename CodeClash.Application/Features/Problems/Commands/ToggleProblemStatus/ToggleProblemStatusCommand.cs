using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.Application.Features.Problems.Commands.ToggleProblemStatus;

public record ToggleProblemStatusCommand(Guid ProblemId) : IRequest<Result<bool>>;

public class ToggleProblemStatusCommandHandler : IRequestHandler<ToggleProblemStatusCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public ToggleProblemStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(ToggleProblemStatusCommand request, CancellationToken ct)
    {
        var problem = await _context.Problems
            .FirstOrDefaultAsync(p => p.Id == request.ProblemId && p.DeletedAt == null, ct);

        if (problem is null)
            return Result<bool>.Failure("Problem not found.");

        if (problem.IsActive)
            problem.Deactivate();
        else
            problem.Activate();

        await _context.SaveChangesAsync(ct);

        return Result<bool>.Success(problem.IsActive, "Problem status toggled successfully.");
    }
}
