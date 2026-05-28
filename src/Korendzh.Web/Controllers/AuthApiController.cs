using System.ComponentModel.DataAnnotations;
using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Korendzh.Web.Controllers;

/// <summary>
/// REST API для мобильного клиента: логин по email+паролю → JWT.
/// Для веба используются стандартные cookie + Razor Pages, JWT там не нужен.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly JwtTokenIssuer _issuer;

    public AuthApiController(UserManager<AppUser> users, SignInManager<AppUser> signIn, JwtTokenIssuer issuer)
    {
        _users = users;
        _signIn = signIn;
        _issuer = issuer;
    }

    public record LoginRequest(
        [Required, EmailAddress] string Email,
        [Required] string Password);

    public record LoginResponse(string Token, DateTime ExpiresAtUtc, string FullName, string[] Roles);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var pwOk = await _users.CheckPasswordAsync(user, req.Password);
        if (!pwOk)
        {
            // Lockout в проекте отключён (см. DependencyInjection.cs). Счётчик неудачных попыток не растёт.
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var (token, expiresAt) = await _issuer.IssueAsync(user);
        var roles = (await _users.GetRolesAsync(user)).ToArray();
        return Ok(new LoginResponse(token, expiresAt, user.FullName, roles));
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var roles = await _users.GetRolesAsync(user);
        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            fullName = user.FullName,
            divisionId = user.DivisionId,
            roles
        });
    }
}
