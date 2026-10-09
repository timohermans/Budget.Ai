using System.Globalization;
using Budget.Web.Data;
using Budget.Web.Domain.Transactions;
using Budget.Web.Infrastructure;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

DotEnv.Load(new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks().AddDbContextCheck<BudgetDbContext>();

var endpointString = builder.Configuration["OpenTelemetry:Endpoint"];
if (!string.IsNullOrWhiteSpace(endpointString))
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("Budget_web")
            .AddAttributes(new Dictionary<string, object>
            {
                ["environment"] = builder.Environment.EnvironmentName
            }))
            .AddOtlpExporter(exporter =>
            {
                var headers = builder.Configuration["OpenTelemetry:Headers"];
                ArgumentException.ThrowIfNullOrWhiteSpace(endpointString);
                ArgumentException.ThrowIfNullOrWhiteSpace(headers);
                exporter.Endpoint = new Uri(endpointString);
                exporter.Headers = headers;
                exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
    });
}

var connection = builder.Configuration.GetConnectionString("Budget")
    ?? Environment.GetEnvironmentVariable("BUDGET_DB_CONNECTION") // TODO: Deze kan eigenlijk weg
    ?? "Host=localhost;Database=budget;Username=budget;Password=budget";

builder.Services.AddDbContext<BudgetDbContext>(options =>
    options.UseNpgsql(connection).UseSnakeCaseNamingConvention());

var dataProtectionKeyPath = builder.Configuration.GetRequiredSection("DataProtectionKeyPath").Get<string>();
ArgumentException.ThrowIfNullOrWhiteSpace(dataProtectionKeyPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));

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
        .AddCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events.OnRedirectToLogin = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                if (context.Request.Headers.ContainsKey("HX-Request"))
                {
                    logger.LogDebug("Unauthorized htmx request for {Path}; returning 401", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                logger.LogDebug("Unauthenticated request for {Path}; redirecting to login", context.Request.Path);
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        })
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

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("An unexpected error occurred.");
}));

app.UseForwardedHeaders();

app.UseAuthentication();

if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<TestModeAuthMiddleware>();
}

app.Use(async (context, next) =>
{
    if (context.Request.Headers.ContainsKey("HX-Request")
        && !(context.User.Identity?.IsAuthenticated ?? false))
    {
        context.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogDebug("Unauthorized htmx request for {Path}; returning 401", context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = _ => true }).AllowAnonymous();

app.MapGet("/", () => Results.Redirect("/budget"));

var dbConnectionInfo = new NpgsqlConnectionStringBuilder(connection);
app.Logger.LogInformation(
    "Budget.Web starting in {Environment}; auth mode: {AuthMode}; database: {DbHost}/{DbDatabase} as {DbUsername}",
    app.Environment.EnvironmentName,
    string.IsNullOrEmpty(oidcAuthority) ? "cookie-only" : "oidc",
    dbConnectionInfo.Host, dbConnectionInfo.Database, dbConnectionInfo.Username);

app.Run();
