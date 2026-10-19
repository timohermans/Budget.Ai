using System.Text.RegularExpressions;
using Budget.Scraper;
using Microsoft.Playwright;

var logPath = Environment.GetEnvironmentVariable("BANKING_LOG_PATH")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dev/logs/budget.scraper.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
Console.SetOut(new TimestampedLogWriter(logPath, Console.Out));
Console.SetError(new TimestampedLogWriter(logPath, Console.Error));

using var loggerFactory = Log.Configure();
Log.Information("🚀 Starting bank scrape...");

try
{
    var browserPath = Environment.GetEnvironmentVariable("BANKING_BROWSER_PATH")
        ?? "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
    var cdpPort = Environment.GetEnvironmentVariable("BANKING_CDP_PORT") ?? "9222";

    var downloadDir = Environment.GetEnvironmentVariable("DOWNLOAD_DIR")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Dev", "data", "budget-scraper");
    Directory.CreateDirectory(downloadDir);
    Log.Debug($"Using download directory {downloadDir}");

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

    var destination = Path.Combine(downloadDir, fileName);
    File.Copy(path, destination, overwrite: true);
    Log.Debug($"Copied {fileName} to {destination}");

    var budget = new BudgetPage(page);
    await budget.GotoAsync();

    await Assertions.Expect(page).ToHaveURLAsync(new Regex(@".*(auth\.|geld\.).*$"));
    if (page.Url.Contains("auth"))
    {
        await budget.LoginAsync();
    }

    foreach (var file in Directory.EnumerateFiles(downloadDir))
    {
        var fileToUpload = Path.GetFileName(file);
        try
        {
            await budget.UploadAsync(fileToUpload, file);
            File.Delete(file);
            Log.Information($"Uploaded and removed {fileToUpload}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to upload {fileToUpload}, keeping it in {downloadDir}");
        }
    }


    await page.CloseAsync();
    Log.Information("🏁 Bank scrape finished!");
    return 0;
}
catch (Exception ex)
{
    Log.Error(ex, "💥 Something unexpected went wrong :(");
    return 1;
}