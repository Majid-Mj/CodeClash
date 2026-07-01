using CodeClash.API.Common;
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

    // ─────────────────────────────────────────────────────────────
    // GET /api/v1/profile
    // ─────────────────────────────────────────────────────────────
    /// <summary>Returns the current authenticated user's profile.</summary>
    /// <response code="200">Profile retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User profile not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new GetProfileQuery(userId), ct);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<ProfileDto>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<ProfileDto>.Ok(result.Data, result.Message));
    }

    // ─────────────────────────────────────────────────────────────
    // PUT /api/v1/profile
    // ─────────────────────────────────────────────────────────────
    /// <summary>Updates profile information (FullName, PhoneNumber, Username).</summary>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Validation error or username already exists</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequestDto dto,
        CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new UpdateProfileCommand(userId, dto), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<ProfileDto>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<ProfileDto>.Ok(result.Data, result.Message));
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/v1/profile/image
    // ─────────────────────────────────────────────────────────────
    /// <summary>Uploads and saves a new user profile image.</summary>
    /// <response code="200">Profile image uploaded successfully</response>
    /// <response code="400">Validation error or invalid/missing file</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfilePicture(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("No file uploaded.", "Validation failed"));

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
            return BadRequest(ApiResponse<string>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<string>.Ok(result.Data, result.Message));
    }

    // ─────────────────────────────────────────────────────────────
    // PUT /api/v1/profile/password
    // ─────────────────────────────────────────────────────────────
    /// <summary>Changes the current user's password.</summary>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Validation error or incorrect current password</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto dto,
        CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new ChangePasswordCommand(userId, dto), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<object>.Ok(null, result.Message));
    }

    // ─────────────────────────────────────────────────────────────
    // DELETE /api/v1/profile
    // ─────────────────────────────────────────────────────────────
    /// <summary>Deletes the authenticated user's account and all associated tokens.</summary>
    /// <response code="200">Account deleted successfully</response>
    /// <response code="400">Error during account deletion</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new DeleteAccountCommand(userId), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<object>.Ok(null, result.Message));
    }
}
