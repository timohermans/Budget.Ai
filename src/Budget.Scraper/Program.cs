using Budget.Scraper;
using Microsoft.Playwright;

Console.WriteLine("============== Starting run =============");

var browserPath = Environment.GetEnvironmentVariable("BANKING_BROWSER_PATH")
    ?? "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser";
var cdpPort = Environment.GetEnvironmentVariable("BANKING_CDP_PORT") ?? "9222";
var profileDirectory = Environment.GetEnvironmentVariable("BANKING_PROFILE_DIRECTORY");

var brave = new BraveBrowser(browserPath, cdpPort, profileDirectory);
var browser = await brave.LaunchOrConnectAsync();
if (browser is null)
{
    return 1;
}

var context = browser.Contexts.FirstOrDefault() ?? await browser.NewContextAsync();
var page = await context.NewPageAsync();
var banking = new BankingPage(page);
await banking.GotoAsync();

try {
await banking.CodeInput().FillAsync(Environment.GetEnvironmentVariable("BANKING_LOGIN_CODE") ?? "", new LocatorFillOptions { Timeout = 5 * 1000 });
} catch (Exception ex)
{
    Console.WriteLine("No need to fill in code. Message: " + ex.Message);
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
Console.WriteLine($"Downloaded file {fileName}");
var path = await download.PathAsync();
Console.WriteLine($"Path to file {fileName} is {path}");

var budget = new BudgetPage(page);
await budget.GotoAsync();

try
{
    await budget.LoginAsync();
}
catch (Exception ex)
{
    Console.WriteLine("Login failed. Continuing... Message: " + ex.Message);
}

await budget.UploadAsync(fileName, path);


Console.WriteLine("Everything done! Successfully uploaded file.");
Console.WriteLine("======== Ending run ===========");
await page.CloseAsync();
return 0;