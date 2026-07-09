using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.SystemLogs.DTOs;
using CodeClash.Application.Features.SystemLogs.Queries.GetSystemLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class SystemLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/v1/admin/system-logs
    [HttpGet("api/v1/admin/system-logs")]
    [ProducesResponseType(typeof(PaginatedList<SystemLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSystemLogs(
        [FromQuery] GetSystemLogsQuery query,
        CancellationToken ct)
    {
        var result = await _mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Message, errors = result.Errors });
        }

        return Ok(result.Data);
    }
}
