using System.Security.Claims;
using System.Text;
using Budget.Web.Data;
using Budget.Web.Features.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Budget.Tests.Features;

[TestClass]
public class UploadControllerTests
{
    private const string UserId = "user-1";
    private const string Header = "\"IBAN/BBAN\",\"Munt\",\"BIC\",\"Volgnr\",\"Datum\",\"Rentedatum\",\"Bedrag\",\"Saldo na trn\",\"Tegenrekening IBAN/BBAN\",\"Naam tegenpartij\",\"Naam uiteindelijke partij\",\"Naam initiërende partij\",\"BIC tegenpartij\",\"Code\",\"Batch ID\",\"Transactiereferentie\",\"Machtigingskenmerk\",\"Incassant ID\",\"Betalingskenmerk\",\"Omschrijving-1\",\"Omschrijving-2\",\"Omschrijving-3\",\"Reden retour\",\"Oorspr bedrag\",\"Oorspr munt\",\"Koers\"";
    private const string Row = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000000001\",\"2026-01-03\",\"2026-01-03\",\"-150,00\",\"\",\"THEIRS1\",\"Albert Heijn\",\"\",\"\",\"\",\"bc\",\"\",\"\",\"\",\"\",\"\",\"Groceries\",\"\",\"\",\"\",\"\",\"\",\"\"";

    private static BudgetDbContext NewDb() => new(
        new DbContextOptionsBuilder<BudgetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static UploadController NewController(BudgetDbContext db)
        => new(
            new RabobankCsvImporter(db, NullLogger<RabobankCsvImporter>.Instance),
            NullLogger<UploadController>.Instance)
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

    private static FormFile CsvFile(string csv)
    {
        var bytes = Encoding.Latin1.GetBytes(csv);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "rabo.csv");
    }

    [TestMethod]
    public async Task Index_WhenNoFile_ThenErrorView()
    {
        await using var db = NewDb();

        var result = await NewController(db).Index(null, CancellationToken.None);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreEqual("Error", view.ViewName);
    }

    [TestMethod]
    public async Task Index_WhenValidCsv_ThenRedirectsToMonthOfLatestTransaction()
    {
        await using var db = NewDb();

        var result = await NewController(db).Index(CsvFile(Header + "\n" + Row), CancellationToken.None);

        var redirect = result as RedirectResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("/budget/2026/1", redirect.Url);
    }

    [TestMethod]
    public async Task Index_WhenValidCsv_ThenImportsRowsForCurrentUser()
    {
        await using var db = NewDb();

        await NewController(db).Index(CsvFile(Header + "\n" + Row), CancellationToken.None);

        var stored = await db.Transactions.SingleAsync();
        Assert.AreEqual(UserId, stored.UserId);
        Assert.AreEqual("Albert Heijn", stored.NameOtherParty);
    }

    [TestMethod]
    public async Task Index_WhenCsvHasNoRows_ThenRedirectsToBudget()
    {
        await using var db = NewDb();

        var result = await NewController(db).Index(CsvFile(Header), CancellationToken.None);

        var redirect = result as RedirectResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("/budget", redirect.Url);
        Assert.IsEmpty(await db.Transactions.ToListAsync());
    }

    [TestMethod]
    public async Task Index_WhenCsvMalformed_ThenErrorView()
    {
        await using var db = NewDb();
        var badRow = Row.Replace("2026-01-03", "not-a-date");

        var result = await NewController(db).Index(CsvFile(Header + "\n" + badRow), CancellationToken.None);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreEqual("Error", view.ViewName);
        Assert.IsEmpty(await db.Transactions.ToListAsync());
    }
}
