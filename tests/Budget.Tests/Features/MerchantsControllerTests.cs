using Budget.Web.Data;
using Budget.Web.Domain.Merchants;
using Budget.Web.Domain.Transactions;
using Budget.Web.Features.Merchants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Tests.Features;

[TestClass]
public class MerchantsControllerTests
{
    private static BudgetDbContext NewDb() => new(
        new DbContextOptionsBuilder<BudgetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<List<MerchantRowModel>> GetRowsAsync(
        BudgetDbContext db, string search = "", string sort = "", string dir = "")
    {
        var controller = new MerchantsRowsController(db);
        var result = await controller.Rows(search, sort, dir, CancellationToken.None);
        var partial = result as PartialViewResult;
        return ((RowsPartialModel)partial!.ViewData.Model!).Rows;
    }

    private static async Task AddTransactionAsync(
        BudgetDbContext db, string name, DateOnly date, int followNumber = 1, string user = "user-1")
    {
        db.Transactions.Add(new Transaction
        {
            Iban = "NL01RABO0000000001",
            IbanOtherParty = "NL99RABO0000000000",
            FollowNumber = followNumber,
            UserId = user,
            Amount = -10m,
            Date = date,
            NameOtherParty = name,
            NameOtherPartyNormalized = MerchantNameNormalizer.Normalize(name),
            Code = "bc",
        });
        await db.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Rows_WhenNoTransactions_ThenEmpty()
    {
        await using var db = NewDb();

        var rows = await GetRowsAsync(db);

        Assert.IsEmpty(rows);
    }

    [TestMethod]
    public async Task Rows_WhenUnmapped_ThenUnmappedFirstOrderedByCountDesc()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3), followNumber: 1);
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 10), followNumber: 2);
        await AddTransactionAsync(db, "Jumbo", new DateOnly(2026, 1, 5), followNumber: 3);
        await AddTransactionAsync(db, "AH", new DateOnly(2026, 1, 7), followNumber: 4);

        var rows = await GetRowsAsync(db);

        Assert.HasCount(3, rows);
        Assert.AreEqual("albert heijn", rows[0].NameNormalized, "Most frequent unmapped name should come first");
        Assert.AreEqual(2, rows[0].TransactionCount);
        Assert.AreEqual(new DateOnly(2026, 1, 3), rows[0].FirstSeen);
        Assert.IsNull(rows[0].Status);
        Assert.IsFalse(rows[0].IsLinked);
        Assert.AreEqual("Albert Heijn", rows[0].RawName);
    }

    [TestMethod]
    public async Task Rows_WhenLinked_ThenShowsLinkedStatusAndTarget()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3));
        await AddTransactionAsync(db, "AH", new DateOnly(2026, 1, 5));

        await new MerchantsMapController(db).Map("Albert Heijn", "Albert Heijn", "https://example.com/ah.png", "", "", "", CancellationToken.None);
        await new MerchantsLinkController(db).Link("AH", "Albert Heijn", "", "", "", CancellationToken.None);

        var rows = await GetRowsAsync(db);

        var linked = rows.Single(r => r.NameNormalized == "ah");
        Assert.IsTrue(linked.IsLinked);
        Assert.AreEqual("Albert Heijn", linked.LinkedToName);
        Assert.AreEqual(MerchantStatus.Mapped, linked.Status);
        Assert.AreEqual("https://example.com/ah.png", linked.LogoUrl);
        Assert.AreEqual("Albert Heijn", linked.DisplayName);
    }

    [TestMethod]
    public async Task Rows_WhenMapped_ThenStatusesReflectState()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3));

        await new MerchantsMapController(db).Map("Albert Heijn", "Albert Heijn", "https://example.com/ah.png", "", "", "", CancellationToken.None);

        var rows = await GetRowsAsync(db);

        Assert.AreEqual(MerchantStatus.Mapped, rows.Single(r => r.NameNormalized == "albert heijn").Status);
    }

    [TestMethod]
    public async Task Rows_WhenSearch_ThenFilters()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3));
        await AddTransactionAsync(db, "Jumbo", new DateOnly(2026, 1, 5));

        var rows = await GetRowsAsync(db, search: "jumbo");

        Assert.HasCount(1, rows);
        Assert.AreEqual("jumbo", rows[0].NameNormalized);
    }

    [TestMethod]
    public async Task Rows_WhenSortByCountAsc_ThenLowestCountFirst()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3), followNumber: 1);
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 10), followNumber: 2);
        await AddTransactionAsync(db, "Jumbo", new DateOnly(2026, 1, 5), followNumber: 3);

        var rows = await GetRowsAsync(db, sort: "count", dir: "asc");

        Assert.AreEqual("jumbo", rows[0].NameNormalized, "Lowest transaction count should sort first ascending");
    }

    [TestMethod]
    public async Task Map_WhenNew_ThenCreatesMappedMerchant()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3));

        await new MerchantsMapController(db).Map("Albert Heijn", "Albert Heijn", "https://example.com/ah.png", "", "", "", CancellationToken.None);

        var merchant = await db.Merchants.SingleAsync();
        Assert.AreEqual("albert heijn", merchant.NameNormalized);
        Assert.AreEqual("Albert Heijn", merchant.DisplayName);
        Assert.AreEqual("https://example.com/ah.png", merchant.LogoUrl);
        Assert.AreEqual(MerchantStatus.Mapped, merchant.Status);
    }

    [TestMethod]
    public async Task Map_WhenExisting_ThenUpdatesMapping()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3));
        await new MerchantsMapController(db).Map("Albert Heijn", "Old Name", "https://example.com/old.png", "", "", "", CancellationToken.None);

        await new MerchantsMapController(db).Map("ALBERT HEIJN", "New Name", "https://example.com/new.png", "", "", "", CancellationToken.None);

        var merchant = await db.Merchants.SingleAsync();
        Assert.AreEqual("New Name", merchant.DisplayName);
        Assert.AreEqual("https://example.com/new.png", merchant.LogoUrl);
        Assert.AreEqual(MerchantStatus.Mapped, merchant.Status);
    }

    [TestMethod]
    public async Task Map_WhenInvalidUrl_ThenNoMerchantCreated()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3));

        await new MerchantsMapController(db).Map("Albert Heijn", "Albert Heijn", "not-a-url", "", "", "", CancellationToken.None);

        Assert.IsFalse(await db.Merchants.AnyAsync());
    }

    [TestMethod]
    public async Task Link_WhenTargetExists_ThenCreatesAlias()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3));
        await AddTransactionAsync(db, "AH", new DateOnly(2026, 1, 5));
        await new MerchantsMapController(db).Map("Albert Heijn", "Albert Heijn", "https://example.com/ah.png", "", "", "", CancellationToken.None);

        await new MerchantsLinkController(db).Link("AH", "Albert Heijn", "", "", "", CancellationToken.None);

        var alias = await db.MerchantAliases.SingleAsync();
        Assert.AreEqual("ah", alias.NameNormalized);
        var target = await db.Merchants.SingleAsync();
        Assert.AreEqual(target.Id, alias.MerchantId);
    }

    [TestMethod]
    public async Task Link_WhenTargetMissing_ThenNoAliasCreated()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "AH", new DateOnly(2026, 1, 5));

        await new MerchantsLinkController(db).Link("AH", "Albert Heijn", "", "", "", CancellationToken.None);

        Assert.IsFalse(await db.MerchantAliases.AnyAsync());
    }

    [TestMethod]
    public async Task Clear_WhenAlias_ThenOnlyAliasRemoved()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3));
        await AddTransactionAsync(db, "AH", new DateOnly(2026, 1, 5));
        await new MerchantsMapController(db).Map("Albert Heijn", "Albert Heijn", "https://example.com/ah.png", "", "", "", CancellationToken.None);
        await new MerchantsLinkController(db).Link("AH", "Albert Heijn", "", "", "", CancellationToken.None);

        await new MerchantsClearController(db).Clear("AH", "", "", "", CancellationToken.None);

        Assert.IsFalse(await db.MerchantAliases.AnyAsync());
        Assert.HasCount(1, await db.Merchants.ToListAsync(), "The canonical merchant should survive clearing an alias");
    }

    [TestMethod]
    public async Task Clear_WhenMerchant_ThenRemovesMerchantAndItsAliases()
    {
        await using var db = NewDb();
        await AddTransactionAsync(db, "Albert Heijn", new DateOnly(2026, 1, 3));
        await AddTransactionAsync(db, "AH", new DateOnly(2026, 1, 5));
        await new MerchantsMapController(db).Map("Albert Heijn", "Albert Heijn", "https://example.com/ah.png", "", "", "", CancellationToken.None);
        await new MerchantsLinkController(db).Link("AH", "Albert Heijn", "", "", "", CancellationToken.None);

        await new MerchantsClearController(db).Clear("Albert Heijn", "", "", "", CancellationToken.None);

        Assert.IsFalse(await db.Merchants.AnyAsync());
        Assert.IsFalse(await db.MerchantAliases.AnyAsync(), "Aliases pointing at the cleared merchant should be removed");
    }

    [TestMethod]
    public async Task Options_WhenNoQuery_ThenMostRecentlyUpdatedFiveFirst()
    {
        await using var db = NewDb();
        var baseTime = DateTimeOffset.UtcNow.AddHours(-10);
        for (var i = 1; i <= 8; i++)
        {
            db.Merchants.Add(new Merchant
            {
                NameNormalized = $"shop{i:D2}",
                DisplayName = $"Shop {i:D2}",
                Status = i % 2 == 0 ? MerchantStatus.Mapped : MerchantStatus.None,
                UpdatedAt = baseTime.AddMinutes(i),
            });
        }
        await db.SaveChangesAsync();
        await AddTransactionAsync(db, "shop02", new DateOnly(2026, 1, 4), followNumber: 2);

        var result = await new MerchantsOptionsController(db).Options("", "", "", "", "", CancellationToken.None);
        var options = ((OptionsPartialModel)((PartialViewResult)result).ViewData.Model!).Options;

        Assert.HasCount(5, options, "The default picker should offer the five most recent merchants");
        Assert.AreEqual("shop08", options[0].NameNormalized, "Most recently updated merchant should come first");
        Assert.AreEqual("shop07", options[1].NameNormalized);
        Assert.AreEqual("shop04", options[4].NameNormalized);
    }

    [TestMethod]
    public async Task Options_WhenNoQuery_ThenRecentlyAliasedMerchantsFirst()
    {
        await using var db = NewDb();
        var baseTime = DateTimeOffset.UtcNow.AddHours(-10);

        var recentlyMapped = new Merchant
        {
            NameNormalized = "hema",
            DisplayName = "HEMA",
            Status = MerchantStatus.Mapped,
            UpdatedAt = baseTime.AddMinutes(9),
        };
        var recentlyAliased = new Merchant
        {
            NameNormalized = "albert heijn",
            DisplayName = "Albert Heijn",
            Status = MerchantStatus.Mapped,
            UpdatedAt = baseTime.AddMinutes(1),
        };
        db.Merchants.AddRange(recentlyMapped, recentlyAliased);
        await db.SaveChangesAsync();
        db.MerchantAliases.Add(new MerchantAlias
        {
            NameNormalized = "ah",
            MerchantId = recentlyAliased.Id,
            CreatedAt = baseTime.AddMinutes(10),
        });
        await db.SaveChangesAsync();

        var result = await new MerchantsOptionsController(db).Options("", "", "", "", "", CancellationToken.None);
        var options = ((OptionsPartialModel)((PartialViewResult)result).ViewData.Model!).Options;

        Assert.AreEqual(
            "albert heijn",
            options[0].NameNormalized,
            "A merchant with a recent alias should rank above a merely recently updated one");
    }

    [TestMethod]
    public async Task Options_WhenFoldedAliases_ThenCountsFoldIntoMerchant()
    {
        await using var db = NewDb();
        var merchant = new Merchant
        {
            NameNormalized = "albert heijn",
            DisplayName = "Albert Heijn",
            Status = MerchantStatus.Mapped,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();
        db.MerchantAliases.Add(new MerchantAlias { NameNormalized = "ah", MerchantId = merchant.Id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        await AddTransactionAsync(db, "albert heijn", new DateOnly(2026, 1, 3), followNumber: 1);
        await AddTransactionAsync(db, "AH", new DateOnly(2026, 1, 5), followNumber: 2);

        var result = await new MerchantsOptionsController(db).Options("", "albert", "", "", "", CancellationToken.None);
        var options = ((OptionsPartialModel)((PartialViewResult)result).ViewData.Model!).Options;

        var option = options.Single();
        Assert.AreEqual("Albert Heijn", option.Label);
        Assert.AreEqual(2, option.TotalTransactions, "Alias transaction count should fold into the merchant");
    }

    [TestMethod]
    public async Task Options_WhenQuery_ThenFilters()
    {
        await using var db = NewDb();
        db.Merchants.AddRange(
            new Merchant { NameNormalized = "jumbo", DisplayName = "Jumbo", Status = MerchantStatus.Mapped, UpdatedAt = DateTimeOffset.UtcNow },
            new Merchant { NameNormalized = "hema", DisplayName = "HEMA", Status = MerchantStatus.Mapped, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var result = await new MerchantsOptionsController(db).Options("", "jumbo", "", "", "", CancellationToken.None);
        var options = ((OptionsPartialModel)((PartialViewResult)result).ViewData.Model!).Options;

        Assert.HasCount(1, options);
        Assert.AreEqual("jumbo", options[0].NameNormalized);
    }
}
