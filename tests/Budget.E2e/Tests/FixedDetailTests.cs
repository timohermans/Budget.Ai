using System.Text.RegularExpressions;
using Budget.E2e.Pages;
using Budget.E2e.Support;
using Microsoft.Playwright;

namespace Budget.E2e.Tests;

[TestClass]
[TestCategory("E2E")]
public class FixedDetailTests : PlaywrightTestBase
{
    private async Task UploadFixedTransactionsAsync()
    {
        var last = TestConstants.LastMonth;
        await UploadCsvAsync([
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(3000m).Code("sb").Named("Employer").DescribedAs("Salary").FollowNumber(1),
            new TestTransactionBuilder().On(new DateOnly(last.Year, last.Month, 15)).Amount(-800m).Code("cb").Named("Insurance Co").DescribedAs("Health insurance").FollowNumber(2),
            new TestTransactionBuilder().On(new DateOnly(TestConstants.Year, TestConstants.Month, 3)).Amount(-150m).Code("bc").Named("Albert Heijn").DescribedAs("Groceries").FollowNumber(3),
        ]);
    }

    [TestMethod]
    public async Task ClickIncome_ShowsFixedIncomeCard_AndUpdatesUrl()
    {
        await UploadFixedTransactionsAsync();

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        await budget.ClickIncomeValueAsync();

        StringAssert.Contains(Page.Url, "fixed=income", "URL should include ?fixed=income after clicking income value");
        await Assertions.Expect(budget.FixedDetailTransactions).ToHaveCountAsync(1);
        await Assertions.Expect(budget.FixedDetailTransactions.First).ToContainTextAsync("Employer");
        await Assertions.Expect(budget.FixedDetailTransactions.First).ToContainTextAsync("3000");
        await Assertions.Expect(Page.GetByTestId("income-loading")).ToBeAttachedAsync();
    }

    [TestMethod]
    public async Task ClickExpenses_ShowsFixedExpensesCard_AndUpdatesUrl()
    {
        await UploadFixedTransactionsAsync();

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        await budget.ClickExpensesValueAsync();

        StringAssert.Contains(Page.Url, "fixed=expenses", "URL should include ?fixed=expenses after clicking expenses value");
        await Assertions.Expect(budget.FixedDetailTransactions).ToHaveCountAsync(1);
        await Assertions.Expect(budget.FixedDetailTransactions.First).ToContainTextAsync("Insurance Co");
        await Assertions.Expect(budget.FixedDetailTransactions.First).ToContainTextAsync("-800");
        await Assertions.Expect(Page.GetByTestId("expenses-loading")).ToBeAttachedAsync();
    }

    [TestMethod]
    public async Task ClickClose_RemovesCard_AndClearsUrlParameter()
    {
        await UploadFixedTransactionsAsync();

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);
        await budget.ClickIncomeValueAsync();

        await budget.CloseFixedDetailAsync();

        StringAssert.DoesNotMatch(Page.Url, new Regex("fixed="), "URL should no longer contain the fixed parameter");
    }

    [TestMethod]
    public async Task NavigateToDifferentMonth_DismissesCard_AndClearsUrlParameter()
    {
        await UploadFixedTransactionsAsync();

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);
        await budget.ClickIncomeValueAsync();

        await budget.ClickNextMonthAsync(TestConstants.FirstDay.AddMonths(1));

        await Assertions.Expect(budget.FixedDetailCard).ToHaveCountAsync(0);
        StringAssert.DoesNotMatch(Page.Url, new Regex("fixed="), "URL should no longer contain the fixed parameter");
    }

    [TestMethod]
    public async Task BrowserBack_DismissesCard_AndRestoresPreviousUrl()
    {
        await UploadFixedTransactionsAsync();

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);
        await budget.ClickIncomeValueAsync();

        await Page.GoBackAsync();
        await Assertions.Expect(budget.FixedDetailCard).ToHaveCountAsync(0, new() { Timeout = 30_000 });

        StringAssert.DoesNotMatch(Page.Url, new Regex("fixed="), "URL should be restored without the fixed parameter");
    }
}
