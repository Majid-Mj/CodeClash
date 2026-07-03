using AspNet.Security.OAuth.GitHub;
using CodeClash.API.Common;
using CodeClash.API.Extensions;
using CodeClash.Application.Features.Auth.Commands.ForgotPassword;
using CodeClash.Application.Features.Auth.Commands.Login;
using CodeClash.Application.Features.Auth.Commands.Logout;
using CodeClash.Application.Features.Auth.Commands.RefreshToken;
using CodeClash.Application.Features.Auth.Commands.Register;
using CodeClash.Application.Features.Auth.Commands.ResetPassword;
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

    // POST /api/v1/auth/register

    [HttpPost("register")]
    [AllowAnonymous]
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

    // POST /api/v1/auth/login

    [HttpPost("login")]
    [AllowAnonymous]
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

        if (result.Data != null)
        {
            AppendRefreshTokenCookie(result.Data.RefreshToken);
        }

        return Ok(ApiResponse<AuthResponseDto>.Ok(result.Data, result.Message));
    }

    // POST /api/v1/auth/logout

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDto dto,
        CancellationToken ct)
    {
        string? token = dto.RefreshToken;
        if (string.IsNullOrEmpty(token))
        {
            Request.Cookies.TryGetValue("refreshToken", out token);
        }

        var cmdDto = new LogoutRequestDto(token ?? string.Empty, dto.AllDevices);
        Guid userId = User.GetUserId();
        var result = await _mediator.Send(new LogoutCommand(cmdDto, userId), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Errors, result.Message));

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });

        return Ok(ApiResponse<object>.Ok(null, result.Message));
    }

    // POST /api/v1/auth/refresh

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out string? refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Refresh token is missing from cookies."));
        }

        var dto = new RefreshTokenRequestDto(refreshToken);
        var result = await _mediator.Send(new RefreshTokenCommand(dto), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(result.Errors, result.Message));

        if (result.Data != null)
        {
            AppendRefreshTokenCookie(result.Data.RefreshToken);
        }

        return Ok(ApiResponse<AuthResponseDto>.Ok(result.Data, result.Message));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(dto), ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<string>.Fail(result.Errors, result.Message));
        return Ok(ApiResponse<string>.Ok(null, result.Message));
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
            RedirectUri = Url.Action(nameof(GitHubSuccess))
        };

        return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("github-success")]
    [AllowAnonymous]
    public async Task<IActionResult> GitHubSuccess(CancellationToken ct)
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
        AppendRefreshTokenCookie(rawRefreshToken);

        string frontendUrl = (_config["App:FrontendUrl"] ?? "http://localhost:4200").TrimEnd('/');
        return Redirect($"{frontendUrl}/auth-success?token={accessToken}&refreshToken={rawRefreshToken}");
    }
    // ─────────────────────────────────────────────────────────────
    // POST /api/v1/auth/reset-password (Verify OTP & Reset)
    // ─────────────────────────────────────────────────────────────
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(dto), ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<string>.Fail(result.Errors, result.Message));

        return Ok(ApiResponse<string>.Ok(null, result.Message));
    }

    private void AppendRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}