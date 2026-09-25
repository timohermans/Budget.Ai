using Microsoft.Playwright;

namespace Budget.Scraper;

public class BankingPage
{
    private readonly IPage _page;

    public BankingPage(IPage page)
    {
        _page = page;
    }

    public async Task GotoAsync()
    {
        await _page.GotoAsync("https://bankieren.rabobank.nl/welcome");
    }

    public ILocator AccountName() => _page.Locator("#account-name");
    public ILocator CodeInput() => _page.Locator("#rass-tin-code-input");
    public ILocator PaymentAccountRowItem() => _page.Locator("feature-product-overview-card-item:first-child").GetByText("Gezamenlijk betaal");
    public ILocator DownloadTransactionsLink() => _page.GetByText("Download transacties");
    public ILocator FromLastDownloadRadioButton() => _page.Locator("[data-test=\"period-radio-last_download\"]").GetByText("Vanaf de laatste download");
    public ILocator DownloadTransactionsSubmit() => _page.Locator("[data-test=\"download-button\"]");
}