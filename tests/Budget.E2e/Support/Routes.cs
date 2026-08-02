namespace Budget.E2e.Support;

public static class Routes
{
    public static string BaseUrl { get; set; } =
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL") ?? "http://localhost:5080";

    public static string Budget() => $"{BaseUrl}/budget/";
    public static string Budget(int year, int month) => $"{BaseUrl}/budget/{year}/{month}";
    public static string Budget(int year, int month, int week) => $"{BaseUrl}/budget/{year}/{month}/{week}";
    public static string Upload => $"{BaseUrl}/transactions/upload";
    public static string ToggleFixed => $"{BaseUrl}/transactions/toggle-fixed";
}
