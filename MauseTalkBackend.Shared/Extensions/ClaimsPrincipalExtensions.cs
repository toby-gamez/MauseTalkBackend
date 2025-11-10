using System.Security.Claims;

namespace MauseTalkBackend.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }
    
    public static string GetUsername(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("username")?.Value 
               ?? throw new UnauthorizedAccessException("Username not found in claims");
    }
    
    public static string GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("email")?.Value 
               ?? throw new UnauthorizedAccessException("Email not found in claims");
    }
}