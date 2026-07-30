using Microsoft.Playwright;

namespace Budget.Playwright.Pages;

public class WeekCardComponent
{
    private readonly IPage _page;
    private readonly int _weekNumber;

    public WeekCardComponent(IPage page, int weekNumber)
    {
        _page = page;
        _weekNumber = weekNumber;
    }

    private ILocator WeekElement => _page.GetByTestId($"week-{_weekNumber}");
    private ILocator SummaryHeader => WeekElement.GetByTestId($"week-{_weekNumber}-summary");

    public async Task<decimal> GetLeftAsync()
    {
        var text = await _page.GetByTestId($"left-week-{_weekNumber}").TextContentAsync();
        return ParseDecimal(text);
    }

    public async Task<decimal> GetSpentAsync()
    {
        var text = await _page.GetByTestId($"spent-week-{_weekNumber}").TextContentAsync();
        return ParseDecimal(text);
    }

    public async Task<decimal> GetBudgetAsync()
    {
        var el = _page.GetByTestId($"budget-week-{_weekNumber}");
        return ParseDecimal(await el.TextContentAsync());
    }

    public async Task<double> GetProgressPercentageAsync()
    {
        var progress = _page.GetByTestId($"progress-week-{_weekNumber}");
        var value = await progress.GetAttributeAsync("value");
        var max = await progress.GetAttributeAsync("max");
        if (decimal.TryParse(value, out var v) && decimal.TryParse(max, out var m) && m > 0)
            return (double)(v / m * 100);
        return 0;
    }

    public async Task ClickHeaderAsync()
    {
        await SummaryHeader.ClickAsync();
        await _page.GetByTestId($"week-{_weekNumber}-transactions").WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    public async Task<bool> IsExpandedAsync()
    {
        var transactions = WeekElement.GetByTestId("transaction");
        return await transactions.CountAsync() > 0;
    }

    public async Task<List<TransactionInfo>> GetTransactionsAsync()
    {
        var rows = await WeekElement.GetByTestId("transaction").AllAsync();
        var result = new List<TransactionInfo>();
        foreach (var row in rows)
        {
            var amountText = await row.GetByTestId("amount").TextContentAsync();
            var name = await row.GetByTestId("name-other-party").TextContentAsync();
            var idAttr = await row.GetAttributeAsync("id");
            var id = idAttr?.Replace("transaction-", "") ?? "";
            var toggleButton = row.GetByTestId("toggle-submit");

            result.Add(new TransactionInfo
            {
                Id = id,
                Amount = ParseDecimal(amountText),
                NameOtherParty = name?.Trim() ?? "",
                HasToggle = await toggleButton.IsVisibleAsync(),
            });
        }
        return result;
    }

    public async Task ClickToggleAsync(string transactionId)
    {
        await _page.GetByTestId($"toggle-{transactionId}").GetByTestId("toggle-submit").ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

    public record TransactionInfo
    {
        public string Id { get; init; } = "";
        public decimal Amount { get; init; }
        public string NameOtherParty { get; init; } = "";
        public bool HasToggle { get; init; }
    }
}
