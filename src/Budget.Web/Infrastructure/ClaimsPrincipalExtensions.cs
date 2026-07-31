using System.Security.Claims;

namespace Budget.Web.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the user id from the <c>sub</c> claim, or an empty string when the claim is absent.</summary>
    /// <param name="principal">The principal to read the user id from.</param>
    public static string GetUserId(this ClaimsPrincipal principal)
        => principal.FindFirstValue("sub") ?? string.Empty;
}
