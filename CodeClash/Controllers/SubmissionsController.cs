using CodeClash.API.Extensions;
using CodeClash.Application.Features.Submissions.Commands.CreateSubmission;
using CodeClash.Application.Features.Submissions.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/submissions")]
[Authorize]
[Produces("application/json")]
public class SubmissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubmissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/v1/submissions
    [HttpPost]
    [ProducesResponseType(typeof(SubmissionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateSubmission(
        [FromBody] CreateSubmissionRequestDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new CreateSubmissionCommand(dto, userId), ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Message, errors = result.Errors });
        }

        return Ok(result.Data);
    }
}
