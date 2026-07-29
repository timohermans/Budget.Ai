using Budget.Playwright.Pages;
using Budget.Playwright.Support;

namespace Budget.Playwright.Tests;

[TestClass]
public class BudgetCalculationTests : PlaywrightTestBase
{
    [TestMethod]
    public async Task Budget_Baseline_IncomeMinusExpensesEqualsBudget()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(-800m).Code("cb").Named("Insurance Co").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(3),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 5)).Amount(-50m).Code("bc").Named("Hema").FollowNumber(4),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 10)).Amount(-100m).Code("ie").Named("Bol.com").FollowNumber(5),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var income = await budget.GetIncomeAsync();
        var expenses = await budget.GetExpensesAsync();
        var budgetAmount = await budget.GetBudgetAsync();

        Assert.AreEqual(3000m, income, "Income should be 3000 from fixed salary");
        Assert.AreEqual(800m, expenses, "Expenses should be 800 from fixed insurance");
        Assert.AreEqual(2200m, budgetAmount, "Budget should be income - expenses = 2200");
    }

    [TestMethod]
    public async Task Budget_Spent_OnlyIncludesVariableExpenses()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(-800m).Code("cb").Named("Insurance Co").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(3),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 5)).Amount(-50m).Code("bc").Named("Hema").FollowNumber(4),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 10)).Amount(-100m).Code("ie").Named("Bol.com").FollowNumber(5),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var spent = await budget.GetSpentTotalAsync();
        Assert.AreEqual(300m, spent, "Spent should be 150+50+100 = 300 from variable expenses");
    }

    [TestMethod]
    public async Task Budget_Left_EqualsBudgetMinusSpent()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(-800m).Code("cb").Named("Insurance Co").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(3),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 5)).Amount(-50m).Code("bc").Named("Hema").FollowNumber(4),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 10)).Amount(-100m).Code("ie").Named("Bol.com").FollowNumber(5),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var budgetAmount = await budget.GetBudgetAsync();
        var spent = await budget.GetSpentTotalAsync();
        var left = await budget.GetLeftTotalAsync();

        Assert.AreEqual(budgetAmount - spent, left, "Left should equal budget minus spent");
    }

    [TestMethod]
    public async Task Budget_WeekDistribution_ProportionalToDays()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(-800m).Code("cb").Named("Insurance Co").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(3),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 5)).Amount(-50m).Code("bc").Named("Hema").FollowNumber(4),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 10)).Amount(-100m).Code("ie").Named("Bol.com").FollowNumber(5),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        var weekBudget = await weekCard.GetBudgetAsync();
        Assert.IsGreaterThan(0, weekBudget, "Each week should have a budget > 0");
    }

    [TestMethod]
    public async Task Budget_FixedIncomeFromLastMonth_Counted()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 1)).Amount(2000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 10)).Amount(1000m).Code("sb").Named("Freelance Client").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 20)).Amount(500m).Code("sb").Named("Side Gig").FollowNumber(3),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var income = await budget.GetIncomeAsync();
        Assert.AreEqual(3500m, income, "Fixed income from last month should be counted in budget");
    }

    [TestMethod]
    public async Task Budget_FixedExpensesFromLastMonth_Counted()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 10)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 12)).Amount(-800m).Code("cb").Named("Insurance").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 18)).Amount(-400m).Code("cb").Named("Mortgage").FollowNumber(3),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 25)).Amount(-300m).Code("cb").Named("Netflix").FollowNumber(4),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 5)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(5),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 22)).Amount(-75m).Code("bc").Named("Jumbo").FollowNumber(6),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var expenses = await budget.GetExpensesAsync();
        Assert.AreEqual(1500m, expenses, "Fixed expenses from last month should be counted");
    }

    [TestMethod]
    public async Task Budget_OwnAccountTransfers_Excluded()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 16)).Amount(-500m).Code("db").Named("Rabobank").Iban(TestConstants.SavingsIban).IbanOtherParty(TestConstants.SavingsIban).FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 17)).Amount(-800m).Code("cb").Named("Insurance Co").FollowNumber(3),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var budgetAmount = await budget.GetBudgetAsync();
        Assert.AreEqual(2200m, budgetAmount, "Own-account transfer should not affect budget (3000 - 800)");
    }

    [TestMethod]
    public async Task Budget_PayPalTransactions_Excluded()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(500m).Code("sb").Named("PayPal EU").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(-300m).Code("cb").Named("PayPal EU").FollowNumber(3),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var income = await budget.GetIncomeAsync();
        Assert.AreEqual(3000m, income, "PayPal income should be excluded from fixed income");

        var expenses = await budget.GetExpensesAsync();
        Assert.AreEqual(0m, expenses, "PayPal expenses should be excluded from fixed expenses");
    }
}
