using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The current user's id, taken from the validated token ("sub" claim) — never from the request body.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new UnauthorizedException("Invalid token.");
}
