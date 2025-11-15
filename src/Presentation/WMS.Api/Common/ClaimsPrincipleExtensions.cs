using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace WMS.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var subClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(subClaim))
        {
            subClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
        {
            throw new InvalidOperationException("User ID claim ('sub' or 'NameIdentifier') is missing or invalid in the token.");
        }

        return userId;
    }
}