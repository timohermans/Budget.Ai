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
        var result = await controller.Index(Year, Month, null, CancellationToken.None);
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
}
