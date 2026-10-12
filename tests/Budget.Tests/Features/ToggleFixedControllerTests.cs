using System.Globalization;
using System.Security.Claims;
using Budget.Web.Data;
using Budget.Web.Domain.Transactions;
using Budget.Web.Features.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Budget.Tests.Features;

[TestClass]
public class ToggleFixedControllerTests
{
    private const string UserId = "user-1";

    private static BudgetDbContext NewDb() => new(
        new DbContextOptionsBuilder<BudgetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ToggleFixedController NewController(BudgetDbContext db, string userId = UserId)
        => new(db, NullLogger<ToggleFixedController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[] { new Claim("sub", userId) }, "test")),
                },
            },
        };

    private static async Task<Transaction> AddTransactionAsync(
        BudgetDbContext db, string code, decimal amount, DateOnly date, int followNumber, string userId = UserId)
    {
        var transaction = new Transaction
        {
            Iban = "NL01RABO0000000001",
            IbanOtherParty = "NL99RABO0000000000",
            FollowNumber = followNumber,
            UserId = userId,
            Amount = amount,
            Date = date,
            NameOtherParty = "Shop",
            NameOtherPartyNormalized = "shop",
            Code = code,
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction;
    }

    [TestMethod]
    public async Task Index_WhenTransactionMissing_ThenNotFound()
    {
        await using var db = NewDb();

        var result = await NewController(db).Index(999, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Index_WhenTransactionBelongsToOtherUser_ThenNotFoundAndUnchanged()
    {
        await using var db = NewDb();
        var other = await AddTransactionAsync(db, "ei", -20m, new DateOnly(2026, 10, 5), 1, userId: "user-2");

        var result = await NewController(db, UserId).Index(other.Id, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundResult>(result);
        Assert.IsFalse((await db.Transactions.SingleAsync(t => t.Id == other.Id)).IsNotFixed);
    }

    [TestMethod]
    public async Task Index_WhenToggled_ThenFlagPersisted()
    {
        await using var db = NewDb();
        var transaction = await AddTransactionAsync(db, "ei", -20m, new DateOnly(2026, 10, 5), 1);

        await NewController(db).Index(transaction.Id, CancellationToken.None);

        Assert.IsTrue((await db.Transactions.SingleAsync(t => t.Id == transaction.Id)).IsNotFixed);
    }

    [TestMethod]
    public async Task Index_WhenToggledTwice_ThenRestoresFixedFlag()
    {
        await using var db = NewDb();
        var transaction = await AddTransactionAsync(db, "ei", -20m, new DateOnly(2026, 10, 5), 1);

        await NewController(db).Index(transaction.Id, CancellationToken.None);
        await NewController(db).Index(transaction.Id, CancellationToken.None);

        Assert.IsFalse((await db.Transactions.SingleAsync(t => t.Id == transaction.Id)).IsNotFixed);
    }

    [TestMethod]
    public async Task Index_WhenToggled_ThenReturnsToggleFixedPartial()
    {
        await using var db = NewDb();
        var transaction = await AddTransactionAsync(db, "ei", -20m, new DateOnly(2026, 10, 5), 1);

        var result = await NewController(db).Index(transaction.Id, CancellationToken.None);

        var partial = result as PartialViewResult;
        Assert.IsNotNull(partial);
        Assert.AreEqual("_ToggleFixed", partial.ViewName);
        var model = (ToggleFixedPartialModel)partial.Model!;
        Assert.AreEqual(transaction.Id, model.Transaction.Id);
        Assert.IsFalse(model.Transaction.IsFixed, "A transaction toggled off fixed should no longer be classified as fixed");
        Assert.IsTrue(model.Transaction.IsNotFixed);
    }

    [TestMethod]
    public async Task Index_WhenFixedExpenseToggled_ThenSpentAndWeekRecalculated()
    {
        await using var db = NewDb();
        var date = new DateOnly(2026, 10, 5);
        await AddTransactionAsync(db, "bc", -10m, date, followNumber: 1);
        var fixedExpense = await AddTransactionAsync(db, "ei", -20m, date, followNumber: 2);

        var result = await NewController(db).Index(fixedExpense.Id, CancellationToken.None);

        var model = (ToggleFixedPartialModel)((PartialViewResult)result).Model!;
        Assert.AreEqual(30m, model.Spent, "Toggling a fixed expense off should count it as variable spending");
        Assert.AreEqual(30m, model.WeekSpent);
        Assert.AreEqual(ISOWeek.GetWeekOfYear(date), model.WeekNumber);
    }
}
