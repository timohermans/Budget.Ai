using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace Budget.Scraper;

public static class Log
{
    private static ILogger? _logger;

    public static ILoggerFactory Configure()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole();
            builder.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("Budget_scraper")
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["environment"] = "Production",
                            ["service.version"] = "1.0.0"
                        }))
                        // .AddConsoleExporter() // only enable to debug opentelemetry itself
                        .AddOtlpExporter(exporter =>
                        {
                            var endpointString = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
                            var headers = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
                            ArgumentException.ThrowIfNullOrWhiteSpace(endpointString);
                            ArgumentException.ThrowIfNullOrWhiteSpace(headers);
                            exporter.Endpoint = new Uri(endpointString);
                            exporter.Headers = headers;
                            exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
                        });
            });
        });

        _logger = loggerFactory.CreateLogger("Budget.Scraper");
        return loggerFactory;

    }

    public static void Information(string message, params object?[] args) => _logger!.LogInformation(message, args);

    public static void Debug(string message, params object?[] args) => _logger!.LogDebug(message, args);

    public static void Debug(Exception ex, string message, params object?[] args) => _logger!.LogDebug(ex, message, args);

    public static void Error(string message, params object?[] args) => _logger!.LogError(message, args);

    public static void Error(Exception ex, string message, params object?[] args) => _logger!.LogError(ex, message, args);

    public static void Fatal(string message, params object?[] args) => _logger!.LogCritical(message, args);
}