using System.Security.Claims;
using Budget.Web.Data;
using Budget.Web.Domain.Merchants;
using Budget.Web.Domain.Transactions;
using Budget.Web.Features.Budget;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Tests.Features;

[TestClass]
public class OverviewControllerTests
{
    private const int Year = 2026;
    private const int Month = 1;
    private const string UserId = "user-1";

    private static BudgetDbContext NewDb() => new(
        new DbContextOptionsBuilder<BudgetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static OverviewController NewController(BudgetDbContext db)
    {
        var controller = new OverviewController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[] { new Claim("sub", UserId) }, "test")),
                },
            },
        };
        return controller;
    }

    private static async Task AddTransactionAsync(BudgetDbContext db, string name, DateOnly date, int followNumber)
    {
        db.Transactions.Add(new Transaction
        {
            Iban = "NL01RABO0000000001",
            IbanOtherParty = "NL99RABO0000000000",
            FollowNumber = followNumber,
            UserId = UserId,
            Amount = -10m,
            Date = date,
            NameOtherParty = name,
            NameOtherPartyNormalized = MerchantNameNormalizer.Normalize(name),
            Code = "bc",
        });
        await db.SaveChangesAsync();
    }

    private static async Task<List<TransactionTemplateModel>> GetMonthTransactionsAsync(OverviewController controller)
    {
        var result = await controller.Index(Year, Month, null, null, null, CancellationToken.None);
        var summary = ((OverviewViewModel)((ViewResult)result).Model!).Summary;
        return summary.Weeks.Values
            .SelectMany(w => w.Transactions)
            .Concat(summary.IbanBalances.Values.SelectMany(b => b.Transactions))
            .DistinctBy(t => t.Id)
            .ToList();
    }

    [TestMethod]
    public async Task Index_WhenExactMerchant_ThenLogoAndDisplayNameAttached()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(Year, Month, 5), followNumber: 1);
        db.Merchants.Add(new Merchant
        {
            NameNormalized = "albert heijn",
            DisplayName = "Albert Heijn",
            LogoUrl = "https://example.com/ah.png",
            Status = MerchantStatus.Mapped,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var transactions = await GetMonthTransactionsAsync(NewController(db));

        var row = transactions.Single();
        Assert.AreEqual("https://example.com/ah.png", row.LogoUrl);
        Assert.AreEqual("Albert Heijn", row.DisplayName);
    }

    [TestMethod]
    public async Task Index_WhenAliased_ThenResolvesThroughAlias()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "AH", new DateOnly(Year, Month, 5), followNumber: 1);
        var merchant = new Merchant
        {
            NameNormalized = "albert heijn",
            DisplayName = "Albert Heijn",
            LogoUrl = "https://example.com/ah.png",
            Status = MerchantStatus.Mapped,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();
        db.MerchantAliases.Add(new MerchantAlias { NameNormalized = "ah", MerchantId = merchant.Id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var transactions = await GetMonthTransactionsAsync(NewController(db));

        var row = transactions.Single();
        Assert.AreEqual("https://example.com/ah.png", row.LogoUrl);
        Assert.AreEqual("Albert Heijn", row.DisplayName);
    }

    [TestMethod]
    public async Task Index_WhenUnmapped_ThenPlaceholderStaysEmpty()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Paschen Petra", new DateOnly(Year, Month, 5), followNumber: 1);

        var transactions = await GetMonthTransactionsAsync(NewController(db));

        var row = transactions.Single();
        Assert.IsNull(row.LogoUrl);
        Assert.IsNull(row.DisplayName);
    }

    [TestMethod]
    public async Task Index_WhenFixedParamAndHtmxRequest_ThenReturnsFixedTransactionsPartial()
    {
        await using var db = NewDb();
        var controller = NewController(db);
        controller.HttpContext.Request.Headers["HX-Request"] = "true";

        var result = await controller.Index(Year, Month, null, "income", null, CancellationToken.None);

        var partial = result as PartialViewResult;
        Assert.IsNotNull(partial, "htmx request with fixed param should return a partial view");
        Assert.AreEqual("_FixedTransactions", partial.ViewName);
        var model = partial.Model as OverviewViewModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("income", model.Fixed);
    }

    [TestMethod]
    public async Task Index_WhenFixedParamAndNotHtmxRequest_ThenReturnsFullView()
    {
        await using var db = NewDb();
        var controller = NewController(db);

        var result = await controller.Index(Year, Month, null, "income", null, CancellationToken.None);

        var view = result as ViewResult;
        Assert.IsNotNull(view, "plain request with fixed param should return the full view (bookmark/share scenario)");
        var model = view.Model as OverviewViewModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("income", model.Fixed);
    }

    [TestMethod]
    public async Task Index_WhenFixedParamAbsent_ThenReturnsFullView()
    {
        await using var db = NewDb();
        var controller = NewController(db);
        controller.HttpContext.Request.Headers["HX-Request"] = "true";

        var result = await controller.Index(Year, Month, null, null, null, CancellationToken.None);

        Assert.IsInstanceOfType<ViewResult>(result, "request without fixed param should return the full view");
    }

    private static async Task<Summary> GetSummaryAsync(OverviewController controller, string? sort)
    {
        var result = await controller.Index(Year, Month, null, null, sort, CancellationToken.None);
        return ((OverviewViewModel)((ViewResult)result).Model!).Summary;
    }

    private static async Task AddTransactionAsync(BudgetDbContext db, string name, DateOnly date, int followNumber, decimal amount)
    {
        db.Transactions.Add(new Transaction
        {
            Iban = "NL01RABO0000000001",
            IbanOtherParty = "NL99RABO0000000000",
            FollowNumber = followNumber,
            UserId = UserId,
            Amount = amount,
            Date = date,
            NameOtherParty = name,
            NameOtherPartyNormalized = MerchantNameNormalizer.Normalize(name),
            Code = "bc",
        });
        await db.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Index_WhenSortHoogsteUitgaves_ThenOrdersExpensesLargestFirst()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Salaris", new(Year, 1, 5), 1, 1000m);
        await AddTransactionAsync(db, "Huur", new(Year, 1, 6), 2, -450m);
        await AddTransactionAsync(db, "Albert Heijn", new(Year, 1, 7), 3, -50m);
        await AddTransactionAsync(db, "Bol.com", new(Year, 1, 8), 4, -3.50m);
        await AddTransactionAsync(db, "Terugbetaling", new(Year, 1, 9), 5, 25m);

        var summary = await GetSummaryAsync(NewController(db), "hoogste-uitgaves");

        var amounts = summary.IbanBalances.Values.Single().Transactions.Select(t => t.Amount).ToList();
        CollectionAssert.AreEqual(new List<decimal> { -450m, -50m, -3.50m, 25m, 1000m }, amounts);
    }

    [TestMethod]
    public async Task Index_WhenSortKleinsteUitgaves_ThenOrdersExpensesSmallestFirst()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Salaris", new(Year, 1, 5), 1, 1000m);
        await AddTransactionAsync(db, "Huur", new(Year, 1, 6), 2, -450m);
        await AddTransactionAsync(db, "Albert Heijn", new(Year, 1, 7), 3, -50m);
        await AddTransactionAsync(db, "Bol.com", new(Year, 1, 8), 4, -3.50m);
        await AddTransactionAsync(db, "Terugbetaling", new(Year, 1, 9), 5, 25m);

        var summary = await GetSummaryAsync(NewController(db), "kleinste-uitgaves");

        var amounts = summary.IbanBalances.Values.Single().Transactions.Select(t => t.Amount).ToList();
        CollectionAssert.AreEqual(new List<decimal> { -3.50m, -50m, -450m, 25m, 1000m }, amounts);
    }

    [TestMethod]
    public async Task Index_WhenSortHoogsteUitgaves_ThenAlsoOrdersWeekTransactionLists()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Salaris", new(Year, 1, 5), 1, 1000m);
        await AddTransactionAsync(db, "Huur", new(Year, 1, 6), 2, -450m);
        await AddTransactionAsync(db, "Albert Heijn", new(Year, 1, 7), 3, -50m);

        var summary = await GetSummaryAsync(NewController(db), "hoogste-uitgaves");

        var weekTransactions = summary.Weeks.SelectMany(w => w.Value.Transactions).ToList();
        CollectionAssert.AreEqual(new List<decimal> { -450m, -50m, 1000m }, weekTransactions.Select(t => t.Amount).ToList());
    }

    [TestMethod]
    public async Task Index_WhenSortWinkelAZ_ThenOrdersByNameCaseInsensitive()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "banana", new(Year, 1, 5), 1, -10m);
        await AddTransactionAsync(db, "Apple", new(Year, 1, 6), 2, -10m);
        await AddTransactionAsync(db, "zara", new(Year, 1, 7), 3, -10m);

        var asc = await GetSummaryAsync(NewController(db), "winkel-a-z");
        var desc = await GetSummaryAsync(NewController(db), "winkel-z-a");

        var ascNames = asc.IbanBalances.Values.Single().Transactions.Select(t => t.NameOtherParty).ToList();
        CollectionAssert.AreEqual(new List<string> { "Apple", "banana", "zara" }, ascNames);

        var descNames = desc.IbanBalances.Values.Single().Transactions.Select(t => t.NameOtherParty).ToList();
        CollectionAssert.AreEqual(new List<string> { "zara", "banana", "Apple" }, descNames);
    }

    [TestMethod]
    public async Task Index_WhenSortWinkel_ThenUsesDisplayNameWhenLinked()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "bb", new(Year, 1, 5), 1, -10m);
        await AddTransactionAsync(db, "Apple", new(Year, 1, 6), 2, -10m);
        db.Merchants.Add(new Merchant
        {
            NameNormalized = "bb",
            DisplayName = "Big B",
            Status = MerchantStatus.Mapped,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var summary = await GetSummaryAsync(NewController(db), "winkel-a-z");

        var names = summary.IbanBalances.Values.Single().Transactions.Select(t => t.DisplayName ?? t.NameOtherParty).ToList();
        CollectionAssert.AreEqual(new List<string> { "Apple", "Big B" }, names);
    }

    [TestMethod]
    public async Task Index_WhenSortHasEqualAmounts_ThenTiesBreakByDateDescending()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new(Year, 1, 5), 1, -10m);
        await AddTransactionAsync(db, "Jumbo", new(Year, 1, 6), 2, -10m);

        var summary = await GetSummaryAsync(NewController(db), "hoogste-uitgaves");

        var dates = summary.IbanBalances.Values.Single().Transactions.Select(t => t.Date).ToList();
        CollectionAssert.AreEqual(new List<DateOnly> { new(Year, 1, 6), new(Year, 1, 5) }, dates);
    }

    [TestMethod]
    public async Task Index_WhenSortIsInvalid_ThenFallsBackToDateDescending()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Jan 3", new(Year, 1, 3), 1, -10m);
        await AddTransactionAsync(db, "Jan 7", new(Year, 1, 7), 2, -20m);
        await AddTransactionAsync(db, "Jan 12", new(Year, 1, 12), 3, -30m);

        var summary = await GetSummaryAsync(NewController(db), "garbage");

        var dates = summary.IbanBalances.Values.Single().Transactions.Select(t => t.Date).ToList();
        CollectionAssert.AreEqual(new List<DateOnly> { new(Year, 1, 12), new(Year, 1, 7), new(Year, 1, 3) }, dates);
    }

    [TestMethod]
    public async Task Index_WhenSortDateAsc_ThenOrdersByDateAscending()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Jan 3", new(Year, 1, 3), 1, -10m);
        await AddTransactionAsync(db, "Jan 7", new(Year, 1, 7), 2, -20m);
        await AddTransactionAsync(db, "Jan 12", new(Year, 1, 12), 3, -30m);

        var summary = await GetSummaryAsync(NewController(db), "datum-begin-eind");

        var dates = summary.IbanBalances.Values.Single().Transactions.Select(t => t.Date).ToList();
        CollectionAssert.AreEqual(new List<DateOnly> { new(Year, 1, 3), new(Year, 1, 7), new(Year, 1, 12) }, dates);
    }
}
