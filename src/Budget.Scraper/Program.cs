using System.Text.RegularExpressions;
using Budget.Scraper;
using Microsoft.Playwright;
using Serilog;

var logPath = Environment.GetEnvironmentVariable("BANKING_LOG_PATH")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dev/logs/budget.scraper.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
Console.SetOut(new TimestampedLogWriter(logPath, Console.Out));
Console.SetError(new TimestampedLogWriter(logPath, Console.Error));

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
     .WriteTo.Seq(Environment.GetEnvironmentVariable("SEQ_URL") ?? "", apiKey:Environment.GetEnvironmentVariable("SEQ_KEY") ?? "", controlLevelSwitch: new Serilog.Core.LoggingLevelSwitch())
    .CreateLogger();

Log.Information("🚀 Starting bank scrape...");

try
{
    var browserPath = Environment.GetEnvironmentVariable("BANKING_BROWSER_PATH")
        ?? "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
    var cdpPort = Environment.GetEnvironmentVariable("BANKING_CDP_PORT") ?? "9222";

    await using var chrome = new ChromeBrowser(browserPath, cdpPort);
    await using var browser = await chrome.LaunchOrConnectAsync();
    if (browser is null)
    {
        Log.Fatal("Chrome browser is not running! Please have it open when running this script!");
        return 1;
    }

    var context = browser.Contexts.FirstOrDefault() ?? await browser.NewContextAsync();
    var page = await context.NewPageAsync();
    var banking = new BankingPage(page);
    await banking.GotoAsync();

    await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*(/dashboard|/start)$"));

    if (page.Url.Contains("start"))
    {
        await banking.LoginAsync();
    }

    await Assertions.Expect(banking.AccountName()).ToHaveTextAsync("Gezamenlijk betaal");
    await banking.PaymentAccountRowItem().ClickAsync();
    await banking.DownloadTransactionsLink().ClickAsync();
    await banking.FromLastDownloadRadioButton().ClickAsync();

    var download = await page.RunAndWaitForDownloadAsync(async () =>
    {
        await banking.DownloadTransactionsSubmit().ClickAsync();
    });

    if (download == null)
    {
        throw new NullReferenceException("Nothing downloaded!");
    }

    var fileName = download.SuggestedFilename;
    Log.Debug($"Downloaded file {fileName}");
    var path = await download.PathAsync();
    Log.Debug($"Path to file {fileName} is {path}");

    var budget = new BudgetPage(page);
    await budget.GotoAsync();

    await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*(auth\.|geld\.).*$"));
    if (page.Url.Contains("auth"))
    {
        await budget.LoginAsync();
    }

    await budget.UploadAsync(fileName, path);


    await page.CloseAsync();
    Log.Information("🏁 Bank scrape finished!");
    return 0;
}
catch (Exception ex)
{
    Log.Error(ex, "💥 Something unexpected went wrong :(");
    Log.CloseAndFlush();
    return 1;
}
finally
{
    Log.CloseAndFlush();
}