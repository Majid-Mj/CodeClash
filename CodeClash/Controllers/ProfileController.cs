using CodeClash.API.Extensions;
using CodeClash.Application.Features.Profile.Commands.ChangePassword;
using CodeClash.Application.Features.Profile.Commands.DeleteAccount;
using CodeClash.Application.Features.Profile.Commands.UpdateProfile;
using CodeClash.Application.Features.Profile.Commands.UploadProfilePicture;
using CodeClash.Application.Features.Profile.DTOs;
using CodeClash.Application.Features.Profile.Queries.GetProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/v1/profile
    [HttpGet]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new GetProfileQuery(userId), ct);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Message, errors = result.Errors });

        return Ok(result.Data);
    }

    // PUT /api/v1/profile
    [HttpPut]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequestDto dto,
        CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new UpdateProfileCommand(userId, dto), ct);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message, errors = result.Errors });

        return Ok(result.Data);
    }

    // POST /api/v1/profile/image
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfilePicture(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded.", errors = new[] { "Validation failed" } });

        using var stream = file.OpenReadStream();
        var command = new UploadProfilePictureCommand(
            User.GetUserId(),
            stream,
            file.FileName,
            file.ContentType,
            file.Length
        );

        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message, errors = result.Errors });

        return Ok(new { url = result.Data, message = result.Message });
    }

    // PUT /api/v1/profile/password
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto dto,
        CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new ChangePasswordCommand(userId, dto), ct);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message, errors = result.Errors });

        return Ok(new { message = result.Message });
    }

    // DELETE /api/v1/profile
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new DeleteAccountCommand(userId), ct);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message, errors = result.Errors });

        return Ok(new { message = result.Message });
    }
}
