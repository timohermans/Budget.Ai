using Budget.E2e.Pages;
using Budget.E2e.Support;
using Microsoft.Playwright;

namespace Budget.E2e.Tests;

[TestClass]
[TestCategory("E2E")]
public class MerchantLogoTests : PlaywrightTestBase
{
    private const string LogoUrl = "https://example.com/logo.png";

    private const string TinySvg =
        "<svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'><rect width='1' height='1' fill='green'/></svg>";

    [TestInitialize]
    public async Task MerchantLogoTestInit()
    {
        await Page.RouteAsync("https://example.com/**", route =>
            route.FulfillAsync(new RouteFulfillOptions
            {
                ContentType = "image/svg+xml",
                Body = TinySvg,
            }));
    }

    private static string FreshName() => $"shop{Guid.NewGuid():N}";

    [TestMethod]
    public async Task MerchantLogo_FullFlow_MapsLinksAndRendersOnOverview()
    {
        var mappedName = FreshName();
        var linkedName = FreshName();
        var recentLinkName = FreshName();
        var displayName = $"{mappedName} Store";

        await UploadCsvAsync([
            new TestTransactionBuilder().On(TestConstants.FirstDay).Amount(-42m).Named(mappedName).FollowNumber(1),
            new TestTransactionBuilder().On(TestConstants.FirstDay).Amount(-17m).Named(linkedName).FollowNumber(2),
            new TestTransactionBuilder().On(TestConstants.FirstDay).Amount(-11m).Named(recentLinkName).FollowNumber(3),
        ]);

        var merchants = new MerchantsPage(Page);
        await merchants.GotoAsync();

        await merchants.SearchAsync(mappedName);
        await merchants.MapRowAsync(mappedName, displayName, LogoUrl);
        await merchants.AssertFilteredRowCountAsync(1);

        await merchants.SearchAsync(linkedName);
        await merchants.LinkRowAsync(linkedName, displayName);
        await merchants.AssertFilteredRowCountAsync(1);

        await merchants.SearchAsync(recentLinkName);
        await merchants.LinkToNameAsync(recentLinkName, displayName);
        await merchants.AssertFilteredRowCountAsync(1);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);
        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var transactions = await weekCard.GetTransactionsAsync();

        Assert.HasCount(3, transactions, "All three uploaded transactions should be listed");
        Assert.IsTrue(
            transactions.All(t => t.NameOtherParty == displayName),
            "The display name should replace the raw counterparty name for every linked transaction");
        Assert.IsTrue(
            transactions.All(t => t.HasLogoImage),
            "Every linked transaction should render the circular logo");
    }

    [TestMethod]
    public async Task MerchantLogo_Unmapped_ShowsPlaceholder()
    {
        var unmappedName = FreshName();

        await UploadCsvAsync([
            new TestTransactionBuilder().On(TestConstants.FirstDay).Amount(-9m).Named(unmappedName).FollowNumber(1),
        ]);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);
        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var transactions = await weekCard.GetTransactionsAsync();

        var row = transactions.Single();
        Assert.AreEqual(unmappedName, row.NameOtherParty, "An unmapped transaction should keep its raw name");
        Assert.IsFalse(row.HasLogoImage);
        Assert.IsTrue(row.HasLogoPlaceholder, "An unmapped transaction should show the placeholder");
    }

    [TestMethod]
    public async Task MerchantLogo_EmptyPicker_CreatesNewMerchant()
    {
        var name = FreshName();

        await UploadCsvAsync([
            new TestTransactionBuilder().On(TestConstants.FirstDay).Amount(-31m).Named(name).FollowNumber(1),
        ]);

        var merchants = new MerchantsPage(Page);
        await merchants.GotoAsync();
        await merchants.SearchAsync(name);

        var prefilled = await merchants.OpenEmptyPickerAsync(name, "zzz-no-such-merchant");
        Assert.AreEqual(name, prefilled, "The map form should prefill the display name with the raw counterparty name");

        await merchants.FillAndSubmitMapAsync(name, LogoUrl);

        var budget = new BudgetPage(Page);
        await budget.GotoAsync(TestConstants.Year, TestConstants.Month);
        var weekCard = budget.WeekCard(1);
        await weekCard.ClickHeaderAsync();

        var transactions = await weekCard.GetTransactionsAsync();
        var row = transactions.Single();
        Assert.IsTrue(row.HasLogoImage, "A merchant created from the picker empty state should render its logo");
    }
}
