using CodeClash.Application.Features.Tournaments.Commands.CancelTournament;
using CodeClash.Application.Features.Tournaments.Commands.CreateTournament;
using CodeClash.Application.Features.Tournaments.Commands.DeleteTournament;
using CodeClash.Application.Features.Tournaments.Commands.PublishTournament;
using CodeClash.Application.Features.Tournaments.Commands.UpdateTournament;
using CodeClash.Application.Features.Tournaments.Queries.GetTournamentById;
using CodeClash.Application.Features.Tournaments.Queries.GetTournaments;
using CodeClash.Application.Features.Tournaments.Commands.RegisterUser;
using CodeClash.Application.Features.Tournaments.Commands.UnregisterUser;
using CodeClash.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeClash.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TournamentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TournamentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTournamentCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTournamentCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("ID mismatch");
        }

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteTournamentCommand(id));
        return NoContent();
    }

    [HttpGet]
    [AllowAnonymous] // Assuming everyone can view tournaments
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetTournamentsQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTournamentByIdQuery(id));
        return Ok(result);
    }

    [HttpPatch("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        await _mediator.Send(new PublishTournamentCommand(id));
        return NoContent();
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _mediator.Send(new CancelTournamentCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(Guid id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized();
            
        Guid userId = User.GetUserId();
        await _mediator.Send(new RegisterUserCommand(id, userId));
        return NoContent();
    }

    [HttpDelete("{id:guid}/unregister")]
    [AllowAnonymous]
    public async Task<IActionResult> Unregister(Guid id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized();

        Guid userId = User.GetUserId();
        await _mediator.Send(new UnregisterUserCommand(id, userId));
        return NoContent();
    }

    [HttpGet("{id:guid}/participants")]
    [AllowAnonymous] // Anyone can view participants
    public async Task<IActionResult> GetParticipants(Guid id)
    {
        var result = await _mediator.Send(new CodeClash.Application.Features.Tournaments.Queries.GetTournamentParticipants.GetTournamentParticipantsQuery(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/generate-bracket")]
    public async Task<IActionResult> GenerateBracket(Guid id)
    {
        var result = await _mediator.Send(new CodeClash.Application.Features.Tournaments.Commands.GenerateBracket.GenerateBracketCommand(id));
        return Ok(result);
    }

    [HttpGet("{id:guid}/matches")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMatches(Guid id)
    {
        var result = await _mediator.Send(new CodeClash.Application.Features.Tournaments.Queries.GetTournamentMatches.GetTournamentMatchesQuery(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/matches/{matchId:guid}/result")]
    public async Task<IActionResult> SubmitMatchResult(Guid id, Guid matchId, [FromBody] CodeClash.Application.Features.Tournaments.Commands.SubmitMatchResult.SubmitMatchResultCommand command)
    {
        if (id != command.TournamentId || matchId != command.MatchId)
            return BadRequest("ID mismatch");

        await _mediator.Send(command);
        return NoContent();
    }
}
