using Budget.Playwright.Support;
using Microsoft.Playwright;

namespace Budget.Playwright.Pages;

public class BudgetPage
{
    private readonly IPage _page;

    public BudgetPage(IPage page)
    {
        _page = page;
    }

    public async Task GotoAsync()
    {
        await _page.GotoAsync(Routes.Budget());
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GotoAsync(int year, int month)
    {
        await _page.GotoAsync(Routes.Budget(year, month));
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GotoAsync(int year, int month, int week)
    {
        await _page.GotoAsync(Routes.Budget(year, month, week));
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task GotoAsync(int year, int month, string iban)
    {
        await _page.GotoAsync(Routes.Budget(year, month, iban));
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<string> GetMonthDisplayAsync()
    {
        return await _page.Locator(".stats h2").First.TextContentAsync() ?? "";
    }

    public async Task<decimal> GetBudgetAsync()
    {
        var text = await _page.Locator("[data-testid='budget-value']").TextContentAsync();
        return ParseDecimal(text);
    }

    public async Task<decimal> GetIncomeAsync()
    {
        var text = await _page.Locator("[data-testid='income-value']").TextContentAsync();
        return ParseDecimal(text);
    }

    public async Task<decimal> GetExpensesAsync()
    {
        var text = await _page.Locator("[data-testid='expenses-value']").TextContentAsync();
        return ParseDecimal(text);
    }

    public async Task<decimal> GetSpentTotalAsync()
    {
        var text = await _page.Locator("#spent-total").TextContentAsync();
        return ParseDecimal(text);
    }

    public async Task<decimal> GetLeftTotalAsync()
    {
        var element = _page.Locator("#left-total");
        var text = await element.TextContentAsync();
        if (text != null)
            text = text.Replace("over", "").Trim();
        return ParseDecimal(text);
    }

    public async Task<string> GetCurrentUrlAsync()
    {
        return _page.Url;
    }

    public async Task ClickPreviousMonthAsync()
    {
        var oldUrl = _page.Url;
        await _page.Locator("[data-testid='nav-previous']").ClickAsync();
        await _page.WaitForFunctionAsync("oldUrl => window.location.href !== oldUrl", oldUrl);
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    public async Task ClickNextMonthAsync()
    {
        var oldUrl = _page.Url;
        await _page.Locator("[data-testid='nav-next']").ClickAsync();
        await _page.WaitForFunctionAsync("oldUrl => window.location.href !== oldUrl", oldUrl);
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    public async Task ClickCurrentMonthAsync()
    {
        var navContainer = _page.Locator("[hx-boost='true']").Last;
        var buttons = await navContainer.Locator("a").AllAsync();
        if (buttons.Count >= 2)
        {
            await buttons[1].ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    public WeekCardComponent WeekCard(int weekNumber)
    {
        return new WeekCardComponent(_page, weekNumber);
    }

    public async Task<List<IbanBalanceInfo>> GetIbanBalancesAsync()
    {
        var ibanSections = await _page.Locator("[id^='iban-']").AllAsync();
        var result = new List<IbanBalanceInfo>();
        foreach (var section in ibanSections)
        {
            var id = await section.GetAttributeAsync("id") ?? "";
            var iban = id.Replace("iban-", "");
            var balanceText = await section.Locator("b").TextContentAsync();
            result.Add(new IbanBalanceInfo
            {
                Iban = iban,
                Balance = ParseDecimal(balanceText),
            });
        }
        return result;
    }

    public async Task<IbanBalanceInfo> ClickIbanAsync(string iban)
    {
        var section = _page.Locator($"#iban-{iban}");
        await section.Locator(".summary").ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return new IbanBalanceInfo { Iban = iban };
    }

    public async Task<string?> GetPageErrorAsync()
    {
        var errorSpan = _page.Locator("span").First;
        if (await errorSpan.IsVisibleAsync())
            return await errorSpan.TextContentAsync();
        return null;
    }

    private static decimal ParseDecimal(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var cleaned = text.Replace("€", "").Trim();
        if (cleaned.Contains(',') && cleaned.Contains('.'))
        {
            cleaned = cleaned.Replace(".", "").Replace(",", ".");
        }
        else if (cleaned.Contains(','))
        {
            cleaned = cleaned.Replace(",", ".");
        }
        return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    public record IbanBalanceInfo
    {
        public string Iban { get; init; } = "";
        public decimal Balance { get; init; }
    }
}
