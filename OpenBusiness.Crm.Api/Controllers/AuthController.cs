using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenBusiness.Crm.Application.Auth;
using OpenBusiness.Crm.Api.Services.Auth;

namespace OpenBusiness.Crm.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        return result is null ? BadRequest("Registration failed") : Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserResponse>> GetCurrentUser(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _authService.GetCurrentUserAsync(userId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}