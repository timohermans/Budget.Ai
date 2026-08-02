using System.Globalization;
using System.Net;
using Budget.Web.Data;
using Budget.Web.Domain.Transactions;
using Budget.Web.Infrastructure;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

DotEnv.Load(new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connection = builder.Configuration.GetConnectionString("Budget")
    ?? Environment.GetEnvironmentVariable("BUDGET_DB_CONNECTION") // TODO: Deze kan eigenlijk weg
    ?? "Host=localhost;Database=budget;Username=budget;Password=budget";

builder.Services.AddDbContext<BudgetDbContext>(options =>
    options.UseNpgsql(connection).UseSnakeCaseNamingConvention());

builder.Services.AddScoped<RabobankCsvImporter>();

var oidcAuthority = builder.Configuration["Oidc:Authority"];

if (!string.IsNullOrEmpty(oidcAuthority))
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(options => options.ExpireTimeSpan = TimeSpan.FromHours(8))
        .AddOpenIdConnect(options =>
        {
            options.Authority = oidcAuthority;
            options.ClientId = builder.Configuration["Oidc:ClientId"]!;
            options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
            options.UseTokenLifetime = true;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.MapInboundClaims = false;
            options.Scope.Add("profile");
        });
}
else
{
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options => options.ExpireTimeSpan = TimeSpan.FromHours(8));
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseAuthentication();

if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<TestModeAuthMiddleware>();
}

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/budget"));

app.Run();
