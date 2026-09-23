using Budget.E2e.Pages;
using Budget.E2e.Support;

namespace Budget.E2e.Tests;

[TestClass]
[TestCategory("E2E")]
public class OverviewSortTests : PlaywrightTestBase
{
    private static readonly DateOnly Jan5 = new(TestConstants.Year, 1, 5);
    private static readonly DateOnly Jan6 = new(TestConstants.Year, 1, 6);
    private static readonly DateOnly Jan7 = new(TestConstants.Year, 1, 7);
    private static readonly DateOnly Jan8 = new(TestConstants.Year, 1, 8);
    private static readonly DateOnly Jan9 = new(TestConstants.Year, 1, 9);

    private BudgetPage _budget = null!;

    private async Task SeedAsync()
    {
        await UploadCsvAsync([
            new TestTransactionBuilder().On(Jan5).Amount(1000m).Code("bc").Named("Salaris").FollowNumber(1),
            new TestTransactionBuilder().On(Jan6).Amount(-450m).Code("bc").Named("Huur").FollowNumber(2),
            new TestTransactionBuilder().On(Jan7).Amount(-50m).Code("bc").Named("Albert Heijn").FollowNumber(3),
            new TestTransactionBuilder().On(Jan8).Amount(-3.50m).Code("bc").Named("Bol.com").FollowNumber(4),
            new TestTransactionBuilder().On(Jan9).Amount(25m).Code("bc").Named("Terugbetaling").FollowNumber(5),
        ]);
        _budget = new BudgetPage(Page);
    }

    [TestMethod]
    public async Task Sort_Default_IsDateDescending()
    {
        await SeedAsync();
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month, 2);

        var weekCard = _budget.WeekCard(2);
        await weekCard.ExpectTransactionOrderAsync("Terugbetaling", "Bol.com", "Albert Heijn", "Huur", "Salaris");
        Assert.AreEqual("", await _budget.SortSelect.InputValueAsync(), "Default sort should be the first option");
        Assert.IsFalse((await _budget.GetCurrentUrlAsync()).Contains("sort="), "Default URL should not carry a sort param");
    }

    [TestMethod]
    public async Task Sort_HoogsteUitgaves_OrdersBiggestExpensesFirst()
    {
        await SeedAsync();
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month, 2);

        await _budget.SelectSortAsync("hoogste-uitgaves");

        await _budget.ExpectUrlContainsSortAsync("hoogste-uitgaves");
        await _budget.WeekCard(2).ExpectTransactionOrderAsync("Huur", "Albert Heijn", "Bol.com", "Terugbetaling", "Salaris");
        Assert.IsTrue(await _budget.WeekCard(2).IsExpandedAsync(), "Week 2 should stay expanded after changing sort");
    }

    [TestMethod]
    public async Task Sort_KleinsteUitgaves_OrdersSmallestExpensesFirst()
    {
        await SeedAsync();
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month, 2);

        await _budget.SelectSortAsync("kleinste-uitgaves");

        await _budget.ExpectUrlContainsSortAsync("kleinste-uitgaves");
        await _budget.WeekCard(2).ExpectTransactionOrderAsync("Bol.com", "Albert Heijn", "Huur", "Terugbetaling", "Salaris");
    }

    [TestMethod]
    public async Task Sort_DatumBeginEind_OrdersDateAscending()
    {
        await SeedAsync();
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month, 2);

        await _budget.SelectSortAsync("datum-begin-eind");

        await _budget.ExpectUrlContainsSortAsync("datum-begin-eind");
        await _budget.WeekCard(2).ExpectTransactionOrderAsync("Salaris", "Huur", "Albert Heijn", "Bol.com", "Terugbetaling");
    }

    [TestMethod]
    public async Task Sort_WinkelAZAndZA_OrdersByName()
    {
        await SeedAsync();
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month, 2);

        await _budget.SelectSortAsync("winkel-a-z");
        await _budget.WeekCard(2).ExpectTransactionOrderAsync("Albert Heijn", "Bol.com", "Huur", "Salaris", "Terugbetaling");

        await _budget.SelectSortAsync("winkel-z-a");
        await _budget.WeekCard(2).ExpectTransactionOrderAsync("Terugbetaling", "Salaris", "Huur", "Bol.com", "Albert Heijn");
    }

    [TestMethod]
    public async Task Sort_EqualAmounts_TieBreaksByDateDescending()
    {
        await UploadCsvAsync([
            new TestTransactionBuilder().On(Jan5).Amount(-10m).Code("bc").Named("Albert Heijn").FollowNumber(1),
            new TestTransactionBuilder().On(Jan6).Amount(-10m).Code("bc").Named("Jumbo").FollowNumber(2),
        ]);
        _budget = new BudgetPage(Page);
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month, 2);

        await _budget.SelectSortAsync("hoogste-uitgaves");

        await _budget.WeekCard(2).ExpectTransactionOrderAsync("Jumbo", "Albert Heijn");
    }

    [TestMethod]
    public async Task Sort_InvalidValue_FallsBackToDefault()
    {
        await SeedAsync();
        await _budget.GotoSortedAsync(TestConstants.Year, TestConstants.Month, "garbage");

        await _budget.WeekCard(2).ClickHeaderAsync();
        await _budget.WeekCard(2).ExpectTransactionOrderAsync("Terugbetaling", "Bol.com", "Albert Heijn", "Huur", "Salaris");
        Assert.AreEqual("", await _budget.SortSelect.InputValueAsync(), "Invalid sort should render the default option");
    }

    [TestMethod]
    public async Task Sort_DeepLink_RendersSortedOrder()
    {
        await SeedAsync();
        await _budget.GotoSortedAsync(TestConstants.Year, TestConstants.Month, "winkel-a-z");

        await _budget.WeekCard(2).ClickHeaderAsync();
        await _budget.WeekCard(2).ExpectTransactionOrderAsync("Albert Heijn", "Bol.com", "Huur", "Salaris", "Terugbetaling");
        Assert.AreEqual("winkel-a-z", await _budget.SortSelect.InputValueAsync());
    }

    [TestMethod]
    public async Task Sort_SurvivesMonthNavigation()
    {
        await SeedAsync();
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month, 2);
        await _budget.SelectSortAsync("hoogste-uitgaves");

        await _budget.ClickPreviousMonthAsync(TestConstants.FirstDay.AddMonths(-1));

        await _budget.ExpectUrlContainsSortAsync("hoogste-uitgaves");
        Assert.AreEqual("hoogste-uitgaves", await _budget.SortSelect.InputValueAsync(), "Sort should persist across month navigation");
    }

    [TestMethod]
    public async Task Sort_SurvivesWeekExpansion()
    {
        await SeedAsync();
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month);
        await _budget.SelectSortAsync("winkel-z-a");

        await _budget.WeekCard(2).ClickHeaderAsync();

        await _budget.ExpectUrlContainsSortAsync("winkel-z-a");
        await _budget.WeekCard(2).ExpectTransactionOrderAsync("Terugbetaling", "Salaris", "Huur", "Bol.com", "Albert Heijn");
    }

    [TestMethod]
    public async Task Sort_SurvivesIbanExpansion()
    {
        await SeedAsync();
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month);
        await _budget.SelectSortAsync("hoogste-uitgaves");

        await _budget.ClickIbanHeaderAsync(TestConstants.TestIban);

        await _budget.ExpectUrlContainsSortAsync("hoogste-uitgaves");
        await _budget.ExpectIbanTransactionOrderAsync(TestConstants.TestIban,
            "Huur", "Albert Heijn", "Bol.com", "Terugbetaling", "Salaris");
    }

    [TestMethod]
    public async Task Sort_FixedDetailCard_IsUnaffected()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 20)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 10)).Amount(500m).Code("sb").Named("Extra").FollowNumber(2),
            new TestTransactionBuilder().On(Jan6).Amount(-450m).Code("bc").Named("Huur").FollowNumber(3),
        ]);
        _budget = new BudgetPage(Page);
        await _budget.GotoAsync(TestConstants.Year, TestConstants.Month);
        await _budget.SelectSortAsync("winkel-a-z");

        await _budget.ClickIncomeValueAsync();

        var amounts = new List<string>();
        for (var i = 0; i < await _budget.FixedDetailTransactions.CountAsync(); i++)
            amounts.Add(await _budget.FixedDetailTransactions.Nth(i).GetByTestId("amount").TextContentAsync() ?? "");
        CollectionAssert.AreEqual(new List<string> { "500.00", "3000.00" }, amounts,
            "Fixed detail card keeps its own amount order; winkel-a-z would have put Employer (3000) first");
        Assert.IsFalse((await _budget.GetCurrentUrlAsync()).Contains("sort="), "Fixed detail card URL should not carry the sort");
    }
}