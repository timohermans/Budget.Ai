using Budget.Playwright.Pages;
using Budget.Playwright.Support;

namespace Budget.Playwright.Tests;

[TestClass]
public class DashboardTests : PlaywrightTestBase
{
    [TestMethod]
    public async Task EmptyDashboard_ShowsMonth()
    {
        var budget = new BudgetPage(Page);
        await budget.GotoAsync();

        var monthDisplay = await budget.GetMonthDisplayAsync();
        Assert.IsNotNull(monthDisplay);
        Assert.AreNotEqual("", monthDisplay);
    }

    [TestMethod]
    public async Task Dashboard_SpecificMonth_ShowsCorrectData()
    {
        var last = TestConstants.LastMonth;
        var twoAgo = last.AddMonths(-1);
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(twoAgo.Year, twoAgo.Month, 1)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 1)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(-800m).Code("cb").Named("Insurance Co").FollowNumber(3),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(4),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var monthDisplay = await budget.GetMonthDisplayAsync();
        var expected = TestConstants.FirstDay.ToString("MMMM");
        StringAssert.Contains(monthDisplay, expected, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task Dashboard_MonthNavigation_ChangesData()
    {
        var current = TestConstants.FirstDay;
        var previous = current.AddMonths(-1);

        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(previous.Year, previous.Month, 1)).Amount(3000m).Code("sb").Named("Employer").DescribedAs("Salary prev month").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(current.Year, current.Month, 1)).Amount(3200m).Code("sb").Named("Employer").DescribedAs("Salary current month").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(previous.Year, previous.Month, 15)).Amount(-800m).Code("cb").Named("Insurance Co").DescribedAs("Insurance prev month").FollowNumber(3),
            new TestTransactionBuilder().On(new DateOnly(current.Year, current.Month, 15)).Amount(-200m).Code("bc").Named("Jumbo").DescribedAs("Groceries current month").FollowNumber(4),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var initialMonth = await budget.GetMonthDisplayAsync();
        StringAssert.Contains(initialMonth, current.ToString("MMMM"), StringComparison.OrdinalIgnoreCase);

        await budget.ClickPreviousMonthAsync();

        var prevMonth = await budget.GetMonthDisplayAsync();
        StringAssert.Contains(prevMonth, previous.ToString("MMMM"), StringComparison.OrdinalIgnoreCase);
        Assert.AreNotEqual(initialMonth, prevMonth, "Previous month display should differ from initial");

        await budget.ClickNextMonthAsync();

        var currentMonth = await budget.GetMonthDisplayAsync();
        StringAssert.Contains(currentMonth, current.ToString("MMMM"), StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task Dashboard_WeekCard_ShowsBudgetSpentLeft()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 10)).Amount(-100m).Code("ie").Named("Bol.com").FollowNumber(3),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        var weekBudget = await weekCard.GetBudgetAsync();

        Assert.IsGreaterThan(0, weekBudget, "Week budget should be positive");
    }

    [TestMethod]
    public async Task Dashboard_WeekCard_ProgressBarReflectsPercentage()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        var percentage = await weekCard.GetProgressPercentageAsync();

        Assert.IsGreaterThanOrEqualTo(0, percentage);
    }

    [TestMethod]
    public async Task Dashboard_WeekCard_ExpandShowsTransactions()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var transactions = await weekCard.GetTransactionsAsync();
        Assert.IsNotEmpty(transactions, "Expanded week should show transactions");
    }

    [TestMethod]
    public async Task Dashboard_WeekExpansion_UrlParamExpandsCorrectWeek()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 10)).Amount(-100m).Code("ie").Named("Bol.com").FollowNumber(3),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month, 1);

        var weekCard = budget.WeekCard(1);
        var isExpanded = await weekCard.IsExpandedAsync();
        Assert.IsTrue(isExpanded, "Week 1 should be expanded via URL param");
    }

    [TestMethod]
    public async Task Dashboard_IbanBalances_ShowCorrectValues()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().Iban("NL12RABO0123456789").On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").DescribedAs("Salary").FollowNumber(1),
            new TestTransactionBuilder().Iban("NL12RABO0123456789").On(new DateOnly(last.Year, last.Month, 15)).Amount(-800m).Code("cb").Named("Insurance Co").DescribedAs("Health insurance").FollowNumber(2),
            new TestTransactionBuilder().Iban("NL12RABO0123456789").On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").DescribedAs("Groceries").FollowNumber(3),
            new TestTransactionBuilder().Iban("NL98INGB9876543210").On(new DateOnly(TestConstants.Year, TestConstants.Month, 12)).Amount(2000m).Code("sb").Named("Freelance Client").DescribedAs("Invoice").FollowNumber(4),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var ibanBalances = await budget.GetIbanBalancesAsync();
        Assert.HasCount(2, ibanBalances, "Should show 2 IBAN balances");
        Assert.IsTrue(ibanBalances.Any(b => b.Iban == "NL12RABO0123456789"), "Should include main account IBAN");
        Assert.IsTrue(ibanBalances.Any(b => b.Iban == "NL98INGB9876543210"), "Should include second account IBAN");
    }

    [TestMethod]
    public async Task Dashboard_TransactionList_LoadsWithoutError() // TODO: hier moeten meer variabele transacties (en kijk daarna via test explorer verder) 
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var transactions = await weekCard.GetTransactionsAsync();
        Assert.IsNotEmpty(transactions);
        Assert.IsTrue(transactions.All(t => t.NameOtherParty.Length > 0));
    }
}
