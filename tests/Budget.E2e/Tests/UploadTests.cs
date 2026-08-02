using Budget.E2e.Pages;
using Budget.E2e.Support;

namespace Budget.E2e.Tests;

[TestClass]
public class UploadTests : PlaywrightTestBase
{
    [TestMethod]
    public async Task Upload_ValidCsv_CreatesTransactions()
    {
        var csv = new CsvBuilder()
            .Add(new TestTransaction
            {
                Date = TestConstants.FirstDay,
                Amount = -150m,
                Code = "bc",
                FollowNumber = 1,
                NameOtherParty = "Test Shop",
                Description = "Test purchase"
            })
            .Build();

        var upload = new UploadPage(Page);
        await upload.UploadViaUiAsync(csv);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();
        var transactions = await weekCard.GetTransactionsAsync();

        Assert.IsNotEmpty(transactions, "Transactions should appear after upload");
        Assert.IsTrue(transactions.Any(t => t.Amount < 0), "Expense transaction should appear");
    }

    [TestMethod]
    public async Task Upload_DuplicateCsv_DoesNotCreateDuplicates()
    {
        var csv = new CsvBuilder()
            .Add(new TestTransaction
            {
                Date = TestConstants.FirstDay,
                Amount = -100m,
                Code = "bc",
                FollowNumber = 1,
                NameOtherParty = "Duplicate Shop",
                Description = "Should appear once"
            })
            .Build();

        var upload = new UploadPage(Page);
        await upload.UploadViaUiAsync(csv);
        await upload.UploadViaUiAsync(csv);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);

        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var transactions = await weekCard.GetTransactionsAsync();
        var matches = transactions.Where(t => t.NameOtherParty == "Duplicate Shop").ToList();
        Assert.HasCount(1, matches, "Duplicate upload should not create duplicate transactions");
    }

    [TestMethod]
    public async Task Upload_ValidCsv_NavigatesToBudget()
    {
        var csv = new CsvBuilder()
            .Add(new TestTransaction
            {
                Date = TestConstants.FirstDay,
                Amount = -50m,
                Code = "bc",
                FollowNumber = 1,
                NameOtherParty = "Shop",
                Description = "Test"
            })
            .Build();

        var upload = new UploadPage(Page);
        await upload.UploadViaUiAsync(csv);

        var budget = new BudgetPage(Page);
        var url = await budget.GetCurrentUrlAsync();
        StringAssert.Contains(url, $"{TestConstants.Year}/{TestConstants.Month}");
    }
}
