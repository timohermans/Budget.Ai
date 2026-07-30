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
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByTestId("file-input").SetInputFilesAsync(new FilePayload
        {
            Name = "test.csv",
            MimeType = "text/csv",
            Buffer = Encoding.Latin1.GetBytes(csvContent)
        });

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task UploadInvalidAsync(string content, string filename = "test.txt")
    {
        await _page.GotoAsync(Routes.Budget());
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByTestId("file-input").SetInputFilesAsync(new FilePayload
        {
            Name = filename,
            MimeType = "text/plain",
            Buffer = Encoding.UTF8.GetBytes(content)
        });

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SubmitWithoutFileAsync()
    {
        await _page.GotoAsync(Routes.Budget());
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByTestId("file-input").EvaluateAsync("el => el.style.display = 'block'");
        await _page.GetByTestId("file-input").SetInputFilesAsync(new string[] { });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
