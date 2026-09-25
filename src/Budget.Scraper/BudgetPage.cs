using System.Text;
using Microsoft.Playwright;

namespace Budget.Scraper;

public class BudgetPage
{
    private readonly IPage _page;

    public BudgetPage(IPage page)
    {
        _page = page;
    }

    public async Task GotoAsync()
    {
        await _page.GotoAsync("https://geld.timo-hermans.nl");
    }

    public async Task LoginAsync()
    {
        await _page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("USERNAME") ?? "no username", new LocatorFillOptions { Timeout = 5000 });
        await _page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("PASSWORD") ?? "no password");
        await _page.Locator("#kc-login").ClickAsync();
        await Assertions.Expect(UploadButton()).ToBeVisibleAsync();
    }

    public async Task UploadAsync(string name, string filePath)
    {
        var uploadResponse = _page.WaitForResponseAsync(
            r => r.Url.EndsWith("/transactions/upload", StringComparison.OrdinalIgnoreCase)
                 && r.Request.Method == "POST");

        await _page.GetByTestId("file-input").SetInputFilesAsync(new FilePayload
        {
            Name = "test.csv",
            MimeType = "text/csv",
            Buffer = File.ReadAllBytes(filePath)
        });

        var response = await uploadResponse;

        if (response.Status != 302)
        {
            throw new InvalidOperationException($"Upload failed with status {response.Status}");
        }
    }

    public ILocator UploadButton() => _page.GetByText("Voeg toe");
}
