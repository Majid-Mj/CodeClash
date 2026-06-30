using CodeClash.API.Common;
using CodeClash.API.Extensions;
using CodeClash.Application.Features.Auth.Commands.Login;
using CodeClash.Application.Features.Auth.Commands.Logout;
using CodeClash.Application.Features.Auth.Commands.Register;
using CodeClash.Application.Features.Auth.Commands.RefreshToken;
using CodeClash.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/v1/auth/register
    // ─────────────────────────────────────────────────────────────
    /// <summary>Creates a new user account and sends a verification email.</summary>
    /// <response code="201">Account created — verification email sent</response>
    /// <response code="400">Validation error or email/username already exists</response>
    /// <response code="500">Unexpected server error</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("register")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterCommand(dto), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<string>.Fail(result.Errors, result.Message));

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<string>.Ok(result.Data, result.Message));
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/v1/auth/login
    // ─────────────────────────────────────────────────────────────
    /// <summary>Authenticates a user and returns JWT access + refresh tokens.</summary>
    /// <response code="200">Login successful — tokens returned</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">Invalid credentials, unverified email, or banned account</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto dto,
        CancellationToken ct)
    {
        string? deviceInfo = Request.Headers.UserAgent.ToString();
        var result = await _mediator.Send(new LoginCommand(dto, deviceInfo), ct);

        if (!result.IsSuccess)
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<AuthResponseDto>.Ok(result.Data, result.Message));
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/v1/auth/logout
    // ─────────────────────────────────────────────────────────────
    /// <summary>Revokes the current (or all) session refresh token(s).</summary>
    /// <response code="200">Logged out successfully</response>
    /// <response code="400">Missing refresh token</response>
    /// <response code="401">Invalid or expired access token</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDto dto,
        CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new LogoutCommand(dto, userId), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<object>.Ok(null, result.Message));
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/v1/auth/refresh
    // ─────────────────────────────────────────────────────────────
    /// <summary>Refreshes the access token using a valid refresh token.</summary>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="400">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequestDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(dto), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<AuthResponseDto>.Ok(result.Data, result.Message));
    }
}