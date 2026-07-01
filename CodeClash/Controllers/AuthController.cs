using AspNet.Security.OAuth.GitHub;
using CodeClash.API.Common;
using CodeClash.API.Extensions;
using CodeClash.Application.Features.Auth.Commands.Login;
using CodeClash.Application.Features.Auth.Commands.Logout;
using CodeClash.Application.Features.Auth.Commands.RefreshToken;
using CodeClash.Application.Features.Auth.Commands.Register;
using CodeClash.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly CodeClash.Application.Common.Interfaces.IApplicationDbContext _context;
    private readonly CodeClash.Application.Common.Interfaces.IJwtService _jwtService;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

    public AuthController(
        IMediator mediator,
        CodeClash.Application.Common.Interfaces.IApplicationDbContext context,
        CodeClash.Application.Common.Interfaces.IJwtService jwtService,
        Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _mediator = mediator;
        _context = context;
        _jwtService = jwtService;
        _config = config;
    }
    //Checks

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



    /// <summary>
    /// Auth Using GitHub
    /// </summary>
    /// <returns></returns>
    [HttpGet("github-login")]
    public IActionResult GitHubLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GitHubCallback))
        };

        return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
    }


    [HttpGet("github-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GitHubCallback(CancellationToken ct)
    {
        // 1. Authenticate using the Cookie scheme (populated by AddGitHub OAuth handler)
        var result = await HttpContext.AuthenticateAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal == null)
        {
            return BadRequest(ApiResponse<object>.Fail("GitHub authentication failed or was cancelled.", "OAuthError"));
        }

        // 2. Extract Claims
        var claims = result.Principal.Claims;
        var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                    ?? result.Principal.FindFirst("urn:github:email")?.Value;
        var name = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value 
                   ?? result.Principal.FindFirst("urn:github:name")?.Value 
                   ?? "GitHub User";
        var githubId = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                       ?? result.Principal.FindFirst("urn:github:id")?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(ApiResponse<object>.Fail("Could not retrieve email from GitHub profile.", "OAuthError"));
        }

        // 3. User Upsert (Lookup by email, or create new)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

        if (user == null)
        {
            // Generate a unique username
            var username = result.Principal.FindFirst("urn:github:login")?.Value 
                           ?? email.Split('@')[0] 
                           ?? Guid.NewGuid().ToString("N").Substring(0, 8);

            var baseUsername = username;
            int counter = 1;
            while (await _context.Users.AnyAsync(u => u.Username == username.ToLower(), ct))
            {
                username = $"{baseUsername}{counter++}";
            }

            user = CodeClash.Domain.Entities.User.CreateGitHub(name, username, email, githubId ?? string.Empty);
            await _context.Users.AddAsync(user, ct);
            await _context.SaveChangesAsync(ct);
        }
        else
        {
            // If user exists but doesn't have GithubId, update it
            if (string.IsNullOrEmpty(user.GithubId))
            {
                user.LinkGitHub(githubId ?? string.Empty);
                _context.Users.Update(user);
                await _context.SaveChangesAsync(ct);
            }
        }

        // 4. Generate standard JWT tokens
        string accessToken = _jwtService.GenerateAccessToken(user);
        string rawRefreshToken = _jwtService.GenerateRawRefreshToken();
        string hashedRefreshToken = _jwtService.HashToken(rawRefreshToken);

        // 5. Persist the Refresh Token
        int expiryDays = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        string deviceInfo = Request.Headers.UserAgent.ToString();
        var refreshToken = CodeClash.Domain.Entities.RefreshToken.Create(hashedRefreshToken, user.Id, expiryDays, deviceInfo);
        
        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // 6. Redirect to Angular Application
        string frontendUrl = _config["App:FrontendUrl"] ?? "http://localhost:4200";
        return Redirect($"{frontendUrl}/auth-success?token={accessToken}&refreshToken={rawRefreshToken}");
    }


}