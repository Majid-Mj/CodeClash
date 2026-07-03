using CodeClash.API.Common;
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
    // Public for Users (active problems only), Admin sees all including inactive

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
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

        var result = await _mediator.Send(new GetProblemsQuery(
            pageNumber,
            pageSize,
            difficulty,
            category,
            search,
            ActiveOnly: !isAdmin), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<object>.Ok(result.Data, result.Message));
    }

    // GET /api/v1/problems/{problemId}
    
    [HttpGet("{problemId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProblemDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemById(
        Guid problemId,
        CancellationToken ct = default)
    {
        bool isAdmin = User.IsInRole("Admin");

        var result = await _mediator.Send(
            new GetProblemByIdQuery(problemId, isAdmin), ct);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<ProblemDetailDto>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<ProblemDetailDto>.Ok(result.Data, result.Message));
    }

    // POST /api/v1/problems          [Admin only]

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("admin-write")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProblem(
        [FromBody] CreateProblemRequestDto dto,
        CancellationToken ct = default)
    {
        Guid adminUserId = User.GetUserId();

        var result = await _mediator.Send(new CreateProblemCommand(dto, adminUserId), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<Guid>.Fail(result.Errors, result.Message));

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<Guid>.Ok(result.Data, result.Message));
    }

    // PUT /api/v1/problems/{problemId}    [Admin only]

    [HttpPut("{problemId:guid}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("admin-write")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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
                ? NotFound(ApiResponse<Guid>.Fail(result.Errors, result.Message))
                : BadRequest(ApiResponse<Guid>.Fail(result.Errors, result.Message));
        }

        return Ok(ApiResponse<Guid>.Ok(result.Data, result.Message));
    }

    // DELETE /api/v1/problems/{problemId}    [Admin only]
  

    [HttpDelete("{problemId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProblem(
        Guid problemId,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteProblemCommand(problemId), ct);

        if (!result.IsSuccess)
        {
            return result.Errors.Any(e => e.Contains("not found"))
                ? NotFound(ApiResponse<object>.Fail(result.Errors, result.Message))
                : BadRequest(ApiResponse<object>.Fail(result.Errors, result.Message));
        }

        return Ok(ApiResponse<object>.Ok(null, result.Message));
    }

    // PUT /api/v1/problems/{problemId}/toggle-status    [Admin only]
    [HttpPut("{problemId:guid}/toggle-status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleProblemStatus(Guid problemId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CodeClash.Application.Features.Problems.Commands.ToggleProblemStatus.ToggleProblemStatusCommand(problemId), ct);

        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<bool>.Fail(result.Errors, result.Message));
        }

        return Ok(ApiResponse<bool>.Ok(result.Data, result.Message));
    }
}