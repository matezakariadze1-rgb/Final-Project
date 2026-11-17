using System.Security.Claims;
using SpaceHub.Application.Exceptions;
namespace SpaceHub.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue("sub"), out var id) && id > 0 ? id
            : throw new BusinessException(FailureKind.Unauthorized, "Invalid user identity.");
}
