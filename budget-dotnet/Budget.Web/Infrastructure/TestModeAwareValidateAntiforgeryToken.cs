using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Budget.Web.Infrastructure;

/// <summary>Validates the antiforgery token on a request, unless the request is running in test mode.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TestModeAwareValidateAntiforgeryToken : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>Validates the antiforgery request token unless the request is authenticated in test mode.</summary>
    /// <param name="context">The current authorization filter context.</param>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Items.ContainsKey(TestModeAuthMiddleware.TestModeKey))
            return;

        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new AntiforgeryValidationFailedResult();
        }
    }
}
