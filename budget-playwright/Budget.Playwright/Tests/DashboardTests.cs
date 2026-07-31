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

        Assert.AreEqual(0m, await budget.GetBudgetAsync(), "Empty budget should be zero");
        Assert.AreEqual(0m, await budget.GetSpentTotalAsync(), "Empty spent total should be zero");
        Assert.AreEqual(0m, await budget.GetLeftTotalAsync(), "Empty left total should be zero");
    }

    [TestMethod]
    public async Task Dashboard_SpecificMonth_ShowsCorrectData()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(-800m).Code("cb").Named("Insurance Co").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(3),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var monthDisplay = await budget.GetMonthDisplayAsync();
        var expected = TestConstants.FirstDay.ToString("MMMM");
        StringAssert.Contains(monthDisplay, expected, StringComparison.OrdinalIgnoreCase);

        Assert.AreEqual(3000m, await budget.GetIncomeAsync(), "Income should be the fixed salary");
        Assert.AreEqual(800m, await budget.GetExpensesAsync(), "Expenses should be the fixed insurance");
        Assert.AreEqual(2200m, await budget.GetBudgetAsync(), "Budget should be income - expenses");
        Assert.AreEqual(150m, await budget.GetSpentTotalAsync(), "Spent should be the variable expense");
        Assert.AreEqual(2050m, await budget.GetLeftTotalAsync(), "Left should be budget - spent");
    }

    [TestMethod]
    public async Task Dashboard_MonthNavigation_ChangesData()
    {
        var current = TestConstants.FirstDay;
        var previous = current.AddMonths(-1);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var initialMonth = await budget.GetMonthDisplayAsync();
        StringAssert.Contains(initialMonth, current.ToString("MMMM"), StringComparison.OrdinalIgnoreCase);

        await budget.ClickPreviousMonthAsync(previous);

        var prevMonth = await budget.GetMonthDisplayAsync();
        StringAssert.Contains(prevMonth, previous.ToString("MMMM"), StringComparison.OrdinalIgnoreCase);
        Assert.AreNotEqual(initialMonth, prevMonth, "Previous month display should differ from initial");

        await budget.ClickNextMonthAsync(current);

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
        var weekSpent = await weekCard.GetSpentAsync();
        var weekLeft = await weekCard.GetLeftAsync();

        Assert.AreEqual(387.10m, weekBudget, $"Week budget should be 387,10 in {TestConstants.Year}-{TestConstants.Month}");
        Assert.AreEqual(150m, weekSpent, "Week spent should include the Albert Heijn expense");
        Assert.AreEqual(237.10m, weekLeft, "Week left should be week budget minus week spent");
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

        Assert.AreEqual(38.75, percentage, "Percentage in week one of 2099-01 should be 150/387,10");
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

        var main = ibanBalances.Single(b => b.Iban == "NL12RABO0123456789");
        Assert.AreEqual(-150m, main.Balance, "Main account balance should be net of this month's transactions");

        var second = ibanBalances.Single(b => b.Iban == "NL98INGB9876543210");
        Assert.AreEqual(2000m, second.Balance, "Second account balance should be net of this month's transactions");
    }

    [TestMethod]
    public async Task Dashboard_TransactionList_ShowsTransactionsPerWeek()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 5)).Amount(-75m).Code("bc").Named("Jumbo").FollowNumber(3),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 10)).Amount(-100m).Code("ie").Named("Bol.com").FollowNumber(4),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 12)).Amount(-50m).Code("ie").Named("Coolblue").FollowNumber(5),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 7)).Amount(-200m).Code("cb").Named("Insurance Co").FollowNumber(6),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month, 1);
        var week1 = budget.WeekCard(1);
        var week1Transactions = await week1.GetTransactionsAsync();
        Assert.HasCount(1, week1Transactions, "Week 1 should have 1 transaction");
        Assert.AreEqual(-150m, week1Transactions[0].Amount);
        Assert.AreEqual("Albert Heijn", week1Transactions[0].NameOtherParty);
        Assert.AreEqual("03-01", week1Transactions[0].Date);
        Assert.IsFalse(week1Transactions[0].HasToggle, "Albert Heijn is variable, no toggle");

        await budget.GotoAsync(TestConstants.Year, TestConstants.Month, 2);
        var week2 = budget.WeekCard(2);
        var week2Transactions = await week2.GetTransactionsAsync();
        Assert.HasCount(3, week2Transactions, "Week 2 should have 3 transactions");
        var jumbo = week2Transactions.Single(t => t.NameOtherParty == "Jumbo");
        Assert.AreEqual(-75m, jumbo.Amount);
        Assert.AreEqual("05-01", jumbo.Date);
        Assert.IsFalse(jumbo.HasToggle, "Jumbo is variable, no toggle");
        var bol = week2Transactions.Single(t => t.NameOtherParty == "Bol.com");
        Assert.AreEqual(-100m, bol.Amount);
        Assert.AreEqual("10-01", bol.Date);
        Assert.IsFalse(bol.HasToggle, "Bol.com is variable, no toggle");
        var insurance = week2Transactions.Single(t => t.NameOtherParty == "Insurance Co");
        Assert.AreEqual(-200m, insurance.Amount);
        Assert.AreEqual("07-01", insurance.Date);
        Assert.IsTrue(insurance.HasToggle, "Insurance Co is fixed, should have toggle");

        await budget.GotoAsync(TestConstants.Year, TestConstants.Month, 3);
        var week3 = budget.WeekCard(3);
        var week3Transactions = await week3.GetTransactionsAsync();
        Assert.HasCount(1, week3Transactions, "Week 3 should have 1 transaction");
        Assert.AreEqual(-50m, week3Transactions[0].Amount);
        Assert.AreEqual("Coolblue", week3Transactions[0].NameOtherParty);
        Assert.AreEqual("12-01", week3Transactions[0].Date);
        Assert.IsFalse(week3Transactions[0].HasToggle, "Coolblue is variable, no toggle");
    }
}
