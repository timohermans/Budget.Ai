using System.Text;
using Budget.Playwright.Support;
using Microsoft.Playwright;

namespace Budget.Playwright.Pages;

public class UploadPage
{
    private readonly IPage _page;

    public UploadPage(IPage page)
    {
        _page = page;
    }

    public async Task UploadViaUiAsync(string csvContent)
    {
        await _page.GotoAsync(Routes.Budget());

        var uploadResponse = _page.WaitForResponseAsync(
            r => r.Url.EndsWith("/transactions/upload", StringComparison.OrdinalIgnoreCase)
                 && r.Request.Method == "POST");
        var redirectNavigation = _page.WaitForURLAsync("**/budget/*/*");

        await _page.GetByTestId("file-input").SetInputFilesAsync(new FilePayload
        {
            Name = "test.csv",
            MimeType = "text/csv",
            Buffer = Encoding.Latin1.GetBytes(csvContent)
        });

        var response = await uploadResponse;
        Assert.AreEqual(302, response.Status, "Upload should redirect after processing");

        await redirectNavigation;
    }

    public async Task UploadInvalidAsync(string content, string filename = "test.txt")
    {
        await _page.GotoAsync(Routes.Budget());

        var uploadResponse = _page.WaitForResponseAsync(
            r => r.Url.EndsWith("/transactions/upload", StringComparison.OrdinalIgnoreCase)
                 && r.Request.Method == "POST");

        await _page.GetByTestId("file-input").SetInputFilesAsync(new FilePayload
        {
            Name = filename,
            MimeType = "text/plain",
            Buffer = Encoding.UTF8.GetBytes(content)
        });

        await uploadResponse;
    }
}
