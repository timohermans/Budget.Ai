using Budget.E2e.Support;
using Microsoft.Playwright;

namespace Budget.E2e.Pages;

public class MerchantsPage
{
    private readonly IPage _page;

    public MerchantsPage(IPage page)
    {
        _page = page;
    }

    public async Task GotoAsync()
    {
        await _page.GotoAsync(Routes.Merchants());
    }

    public async Task SearchAsync(string query)
    {
        await _page.GetByTestId("merchant-search").FillAsync(query);
        var response = await _page.WaitForResponseAsync(
            r => r.Url.Contains("/merchants/rows", StringComparison.OrdinalIgnoreCase)
                 && r.Request.Method == "GET");
        Assert.AreEqual(200, response.Status, "Rows search should succeed");
    }

    public ILocator Row(string nameNormalized)
        => _page.Locator($"tr[data-merchant-name=\"{nameNormalized}\"]");

    public async Task AssertFilteredRowCountAsync(int expected)
    {
        await Assertions.Expect(_page.GetByTestId("merchant-row")).ToHaveCountAsync(expected);
    }

    public async Task MapRowAsync(string nameNormalized, string displayName, string logoUrl)
    {
        var row = Row(nameNormalized);
        await row.GetByTestId("merchant-map").Locator("summary").First.ClickAsync();
        await row.GetByTestId("merchant-display-name").FillAsync(displayName);
        await row.GetByTestId("merchant-logo-url").FillAsync(logoUrl);
        await row.GetByTestId("merchant-map-submit").ClickAsync();
        await Assertions.Expect(Row(nameNormalized).GetByTestId("merchant-status"))
            .ToContainTextAsync("mapped");
    }

    public async Task LinkRowAsync(string nameNormalized, string targetLabel)
    {
        var row = Row(nameNormalized);
        await OpenLinkDetailsAsync(row);
        await FilterOptionsAsync(row, targetLabel);

        var option = row.GetByTestId("merchant-options").GetByTestId("merchant-option").First;
        await Assertions.Expect(option).ToBeVisibleAsync();
        await option.ClickAsync();
        await Assertions.Expect(Row(nameNormalized).GetByTestId("merchant-status"))
            .ToContainTextAsync("linked");
    }

    public async Task LinkToNameAsync(string nameNormalized, string label)
    {
        var row = Row(nameNormalized);
        await OpenLinkDetailsAsync(row);
        var option = row.GetByTestId("merchant-options").GetByTestId("merchant-option")
            .Filter(new() { HasText = label });
        await Assertions.Expect(option.First).ToBeVisibleAsync();
        await option.First.ClickAsync();
        await Assertions.Expect(Row(nameNormalized).GetByTestId("merchant-status"))
            .ToContainTextAsync("linked");
    }

    public async Task<string> OpenEmptyPickerAsync(string nameNormalized, string noMatchQuery)
    {
        var row = Row(nameNormalized);
        await OpenLinkDetailsAsync(row);
        await FilterOptionsAsync(row, noMatchQuery);

        var create = row.GetByTestId("merchant-options").GetByTestId("merchant-options-create");
        await Assertions.Expect(create).ToBeVisibleAsync();
        await create.ClickAsync();
        return await row.GetByTestId("merchant-display-name").InputValueAsync();
    }

    public async Task FillAndSubmitMapAsync(string nameNormalized, string logoUrl)
    {
        var row = Row(nameNormalized);
        await row.GetByTestId("merchant-logo-url").FillAsync(logoUrl);
        await row.GetByTestId("merchant-map-submit").ClickAsync();
        await Assertions.Expect(Row(nameNormalized).GetByTestId("merchant-status"))
            .ToContainTextAsync("mapped");
    }

    private static async Task FilterOptionsAsync(ILocator row, string query)
    {
        await row.GetByTestId("merchant-options-search").FillAsync(query);
        var response = await row.Page.WaitForResponseAsync(
            r => r.Url.Contains("/merchants/options", StringComparison.OrdinalIgnoreCase)
                 && r.Request.Method == "GET");
        Assert.AreEqual(200, response.Status, "Options search should succeed");
    }

    private static async Task OpenLinkDetailsAsync(ILocator row)
    {
        var details = row.GetByTestId("merchant-link");
        if (!await details.GetByTestId("merchant-options").IsVisibleAsync())
            await details.Locator("summary").First.ClickAsync();
    }
}
