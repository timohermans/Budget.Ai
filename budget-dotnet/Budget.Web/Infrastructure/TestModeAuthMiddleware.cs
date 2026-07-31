using System.Security.Claims;

namespace Budget.Web.Infrastructure;

/// <summary>
/// Authenticates requests that carry an <c>X-Test-User</c> header as that user, bypassing OIDC entirely.
/// Only registered in the development environment; the header value is used directly as the user id.
/// </summary>
public class TestModeAuthMiddleware(RequestDelegate next)
{
    /// <summary>The <see cref="HttpContext.Items"/> key that marks a request as running in test mode.</summary>
    public const string TestModeKey = "TestMode";

    /// <summary>Sets the test-mode identity and flag when the <c>X-Test-User</c> header is present, then invokes the next middleware.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task Invoke(HttpContext context)
    {
        var testUser = context.Request.Headers["X-Test-User"].FirstOrDefault();
        if (!string.IsNullOrEmpty(testUser))
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim("sub", testUser),
                    new Claim(ClaimTypes.Name, testUser),
                ],
                authenticationType: "TestMode");

            context.User = new ClaimsPrincipal(identity);
            context.Items[TestModeKey] = true;
        }

        await next(context);
    }
}
