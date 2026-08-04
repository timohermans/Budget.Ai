using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Budget.E2e.Pages;

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
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) &&
            decimal.TryParse(max, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var m) &&
            m > 0)
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
            var dateText = await row.GetByTestId("date").TextContentAsync();
            var idAttr = await row.GetAttributeAsync("id");
            var id = idAttr?.Replace("transaction-", "") ?? "";
            var toggleButton = row.GetByTestId("toggle-submit");
            var logoImage = row.GetByTestId("transaction-logo-image");
            var logoPlaceholder = row.GetByTestId("transaction-logo-placeholder");

            result.Add(new TransactionInfo
            {
                Id = id,
                Amount = ParseDecimal(amountText),
                NameOtherParty = name?.Trim() ?? "",
                Date = dateText?.Trim() ?? "",
                HasToggle = await toggleButton.IsVisibleAsync(),
                HasLogoImage = await logoImage.IsVisibleAsync(),
                HasLogoPlaceholder = await logoPlaceholder.IsVisibleAsync(),
            });
        }
        return result;
    }

    public async Task ClickToggleAsync(string transactionId)
    {
        var toggleButton = _page.GetByTestId($"toggle-{transactionId}").GetByTestId("toggle-submit");
        var isNotFixed = await toggleButton.EvaluateAsync<bool>(
            "el => el.classList.contains('is-not-fixed')");

        await toggleButton.ClickAsync();

        var expect = Assertions.Expect(toggleButton);
        if (isNotFixed)
            await expect.Not.ToHaveClassAsync(new Regex("is-not-fixed"));
        else
            await expect.ToHaveClassAsync(new Regex("is-not-fixed"));
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
        public string Date { get; init; } = "";
        public bool HasToggle { get; init; }
        public bool HasLogoImage { get; init; }
        public bool HasLogoPlaceholder { get; init; }
    }
}
