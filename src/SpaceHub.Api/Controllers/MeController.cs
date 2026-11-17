using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceHub.Application.Auth;
using SpaceHub.Application.Exceptions;
namespace SpaceHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    private readonly IAuthService auth;

   
    public MeController(IAuthService auth)
    {
        this.auth = auth;
    }

    [HttpGet]
    public async Task<ActionResult<UserResponse>> Get(CancellationToken ct)
        => Ok(await auth.GetUserAsync(CurrentUserId, ct));
    [HttpPost("roles/{role}")]
    public async Task<ActionResult<AuthResponse>> ActivateRole(string role, CancellationToken ct)
        => Ok(await auth.ActivateRoleAsync(CurrentUserId, role, ct));
    private int CurrentUserId => int.TryParse(User.FindFirstValue("sub"), out var id) && id > 0
        ? id : throw new BusinessException(FailureKind.Unauthorized, "Invalid user identity.");
}
