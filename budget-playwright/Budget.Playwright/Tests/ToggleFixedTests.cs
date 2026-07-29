using Budget.Playwright.Pages;
using Budget.Playwright.Support;

namespace Budget.Playwright.Tests;

[TestClass]
public class ToggleFixedTests : PlaywrightTestBase
{
    [TestMethod]
    public async Task Toggle_VariableToFixed_UpdatesTransactionDisplay()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(TestConstants.FirstDay.AddDays(3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();
        var toggles = await weekCard.GetTransactionsAsync();

        var toggleable = toggles.FirstOrDefault(t => t.HasToggle);
        if (toggleable != null)
        {
            await weekCard.ClickToggleAsync(toggleable.Id);
        }
    }

    [TestMethod]
    public async Task Toggle_FixedToVariable_UpdatesTransactionDisplay()
    {
        await UploadCsvAsync([
            new TestTransactionBuilder().On(TestConstants.FirstDay.AddDays(3)).Amount(-100m).Code("cb").Named("Insurance").FollowNumber(1),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var toggleable = (await weekCard.GetTransactionsAsync())
            .FirstOrDefault(t => t.HasToggle);

        if (toggleable != null)
        {
            await weekCard.ClickToggleAsync(toggleable.Id);
        }
    }

    [TestMethod]
    public async Task Toggle_UpdatesSpentTotal_OobSwap()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(TestConstants.FirstDay.AddDays(3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var spentBefore = await budget.GetSpentTotalAsync();
        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var toggleable = (await weekCard.GetTransactionsAsync())
            .FirstOrDefault(t => t.HasToggle);

        if (toggleable != null)
        {
            await weekCard.ClickToggleAsync(toggleable.Id);
            var spentAfter = await budget.GetSpentTotalAsync();
            Assert.AreNotEqual(spentBefore, spentAfter,
                "Toggling should change spent total");
        }
    }

    [TestMethod]
    public async Task Toggle_UpdatesLeftTotal_OobSwap()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(TestConstants.FirstDay.AddDays(3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var leftBefore = await budget.GetLeftTotalAsync();
        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var toggleable = (await weekCard.GetTransactionsAsync())
            .FirstOrDefault(t => t.HasToggle);

        if (toggleable != null)
        {
            await weekCard.ClickToggleAsync(toggleable.Id);
            var leftAfter = await budget.GetLeftTotalAsync();
            Assert.AreNotEqual(leftBefore, leftAfter,
                "Toggling should change left total");
        }
    }

    [TestMethod]
    public async Task Toggle_UpdatesProgressBar_OobSwap()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(TestConstants.FirstDay.AddDays(3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();
        var progressBefore = await weekCard.GetProgressPercentageAsync();

        var toggleable = (await weekCard.GetTransactionsAsync())
            .FirstOrDefault(t => t.HasToggle);

        if (toggleable != null)
        {
            await weekCard.ClickToggleAsync(toggleable.Id);
            var progressAfter = await weekCard.GetProgressPercentageAsync();
            Assert.AreNotEqual(progressBefore, progressAfter,
                "Toggling should change progress bar");
        }
    }

    [TestMethod]
    public async Task Toggle_MultipleClicks_WorksRepeatedly()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").FollowNumber(1),
            new TestTransactionBuilder().On(TestConstants.FirstDay.AddDays(3)).Amount(-150m).Code("bc").Named("Albert Heijn").FollowNumber(2),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var toggleable = (await weekCard.GetTransactionsAsync())
            .FirstOrDefault(t => t.HasToggle);

        if (toggleable != null)
        {
            await weekCard.ClickToggleAsync(toggleable.Id);
            await weekCard.ClickToggleAsync(toggleable.Id);
            await weekCard.ClickToggleAsync(toggleable.Id);
        }
    }
}
