using Budget.E2e.Pages;
using Budget.E2e.Support;

namespace Budget.E2e.Tests;

[TestClass]
[TestCategory("E2E")]
public class ToggleFixedTests : PlaywrightTestBase
{
    [TestMethod]
    public async Task Toggle_FixedToVariable_UpdatesWeekCardAndBudgetTotals()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(TestConstants.FirstDay.AddDays(3)).Amount(-100m).Code("cb").Named("Insurance").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var toggleable = (await weekCard.GetTransactionsAsync()).Single(t => t.HasToggle);

        var spentTotalBefore = await budget.GetSpentTotalAsync();
        var leftTotalBefore = await budget.GetLeftTotalAsync();
        var weekSpentBefore = await weekCard.GetSpentAsync();
        var weekBudgetBefore = await weekCard.GetBudgetAsync();
        var weekLeftBefore = await weekCard.GetLeftAsync();
        var progressBefore = await weekCard.GetProgressPercentageAsync();

        await weekCard.ClickToggleAsync(toggleable.Id);

        var spentTotalAfter = await budget.GetSpentTotalAsync();
        var leftTotalAfter = await budget.GetLeftTotalAsync();
        var weekSpentAfter = await weekCard.GetSpentAsync();
        var weekBudgetAfter = await weekCard.GetBudgetAsync();
        var weekLeftAfter = await weekCard.GetLeftAsync();
        var progressAfter = await weekCard.GetProgressPercentageAsync();

        Assert.AreEqual(0m, spentTotalBefore);
        Assert.AreEqual(100m, spentTotalAfter, "Spent total should include the toggled expense");
        Assert.AreEqual(3000m, leftTotalBefore);
        Assert.AreEqual(2900m, leftTotalAfter, "Left total should drop by the toggled expense");
        Assert.AreEqual(0m, weekSpentBefore);
        Assert.AreEqual(100m, weekSpentAfter, "Week spent should include the toggled expense");
        Assert.AreEqual(387.1m, weekBudgetBefore);
        Assert.AreEqual(387.1m, weekBudgetAfter, "Week budget should not change when toggling");
        Assert.AreEqual(387.1m, weekLeftBefore);
        Assert.AreEqual(287.1m, weekLeftAfter, "Week left should drop by the toggled expense");
        Assert.AreEqual(0d, progressBefore);
        Assert.AreEqual(25.83d, progressAfter, 0.005,
            "Progress bar should reflect 100/387.10 spent after toggling");
    }

    [TestMethod]
    public async Task Toggle_VariableToFixed_UpdatesWeekCardAndBudgetTotals()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(TestConstants.FirstDay.AddDays(3)).Amount(-100m).Code("cb").Named("Insurance").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var toggleable = (await weekCard.GetTransactionsAsync()).Single(t => t.HasToggle);

        await weekCard.ClickToggleAsync(toggleable.Id);
        var spentTotalBefore = await budget.GetSpentTotalAsync();
        var leftTotalBefore = await budget.GetLeftTotalAsync();
        var weekSpentBefore = await weekCard.GetSpentAsync();
        var weekBudgetBefore = await weekCard.GetBudgetAsync();
        var weekLeftBefore = await weekCard.GetLeftAsync();
        var progressBefore = await weekCard.GetProgressPercentageAsync();

        await weekCard.ClickToggleAsync(toggleable.Id);
        var spentTotalAfter = await budget.GetSpentTotalAsync();
        var leftTotalAfter = await budget.GetLeftTotalAsync();
        var weekSpentAfter = await weekCard.GetSpentAsync();
        var weekBudgetAfter = await weekCard.GetBudgetAsync();
        var weekLeftAfter = await weekCard.GetLeftAsync();
        var progressAfter = await weekCard.GetProgressPercentageAsync();

        Assert.AreEqual(100m, spentTotalBefore, "Spent total should include the toggled expense while variable");
        Assert.AreEqual(0m, spentTotalAfter, "Spent total should exclude the expense once fixed");
        Assert.AreEqual(2900m, leftTotalBefore, "Left total should account for the variable expense");
        Assert.AreEqual(3000m, leftTotalAfter, "Left total should return to budget once fixed");
        Assert.AreEqual(100m, weekSpentBefore, "Week spent should include the toggled expense while variable");
        Assert.AreEqual(0m, weekSpentAfter, "Week spent should exclude the expense once fixed");
        Assert.AreEqual(387.1m, weekBudgetBefore, "Week budget should not change when toggling");
        Assert.AreEqual(387.1m, weekBudgetAfter, "Week budget should not change when toggling");
        Assert.AreEqual(287.1m, weekLeftBefore, "Week left should account for the variable expense");
        Assert.AreEqual(387.1m, weekLeftAfter, "Week left should return to week budget once fixed");
        Assert.AreEqual(25.83d, progressBefore, 0.005,
            "Progress bar should reflect 100/387.10 spent while variable");
        Assert.AreEqual(0d, progressAfter, "Progress bar should be zero once fixed");
    }
}
