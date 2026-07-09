using CodeClash.API.Extensions;
using CodeClash.Application.Features.Problems.Commands.CreateProblem;
using CodeClash.Application.Features.Problems.Commands.DeleteProblem;
using CodeClash.Application.Features.Problems.Commands.UpdateProblem;
using CodeClash.Application.Features.Problems.DTOs;
using CodeClash.Application.Features.Problems.Queries.GetProblemById;
using CodeClash.Application.Features.Problems.Queries.GetProblems;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/problems")]
[Produces("application/json")]
public class ProblemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProblemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/v1/problems
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProblems(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? difficulty = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        // Admins can see inactive problems; regular users / anonymous see active only
        bool isAdmin = User.IsInRole("Admin");
        Guid? userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var result = await _mediator.Send(new GetProblemsQuery(
            pageNumber,
            pageSize,
            difficulty,
            category,
            search,
            ActiveOnly: !isAdmin,
            UserId: userId), ct);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message, errors = result.Errors });

        return Ok(result.Data);
    }

    // GET /api/v1/problems/{problemId}
    [HttpGet("{problemId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProblemDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemById(
        Guid problemId,
        CancellationToken ct = default)
    {
        bool isAdmin = User.IsInRole("Admin");

        var result = await _mediator.Send(
            new GetProblemByIdQuery(problemId, isAdmin), ct);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Message, errors = result.Errors });

        return Ok(result.Data);
    }

    // POST /api/v1/problems
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("admin-write")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProblem(
        [FromBody] CreateProblemRequestDto dto,
        CancellationToken ct = default)
    {
        Guid adminUserId = User.GetUserId();

        var result = await _mediator.Send(new CreateProblemCommand(dto, adminUserId), ct);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message, errors = result.Errors });

        return StatusCode(StatusCodes.Status201Created, new { id = result.Data, message = result.Message });
    }

    // PUT /api/v1/problems/{problemId}
    [HttpPut("{problemId:guid}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("admin-write")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProblem(
        Guid problemId,
        [FromBody] UpdateProblemRequestDto dto,
        CancellationToken ct = default)
    {
        Guid adminUserId = User.GetUserId();

        var result = await _mediator.Send(
            new UpdateProblemCommand(problemId, dto, adminUserId), ct);

        if (!result.IsSuccess)
        {
            return result.Errors.Any(e => e.Contains("not found"))
                ? NotFound(new { message = result.Message, errors = result.Errors })
                : BadRequest(new { message = result.Message, errors = result.Errors });
        }

        return Ok(new { id = result.Data, message = result.Message });
    }

    // DELETE /api/v1/problems/{problemId}
    [HttpDelete("{problemId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProblem(
        Guid problemId,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteProblemCommand(problemId), ct);

        if (!result.IsSuccess)
        {
            return result.Errors.Any(e => e.Contains("not found"))
                ? NotFound(new { message = result.Message, errors = result.Errors })
                : BadRequest(new { message = result.Message, errors = result.Errors });
        }

        return Ok(new { message = result.Message });
    }

    // PUT /api/v1/problems/{problemId}/toggle-status
    [HttpPut("{problemId:guid}/toggle-status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleProblemStatus(Guid problemId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CodeClash.Application.Features.Problems.Commands.ToggleProblemStatus.ToggleProblemStatusCommand(problemId), ct);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Message, errors = result.Errors });
        }

        return Ok(new { status = result.Data, message = result.Message });
    }
}
