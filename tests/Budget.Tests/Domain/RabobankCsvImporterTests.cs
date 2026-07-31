using System.Text;
using Budget.Web.Data;
using Budget.Web.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Budget.Tests.Domain;

[TestClass]
public class RabobankCsvImporterTests
{
    private const string Header = "\"IBAN/BBAN\",\"Munt\",\"BIC\",\"Volgnr\",\"Datum\",\"Rentedatum\",\"Bedrag\",\"Saldo na trn\",\"Tegenrekening IBAN/BBAN\",\"Naam tegenpartij\",\"Naam uiteindelijke partij\",\"Naam initiërende partij\",\"BIC tegenpartij\",\"Code\",\"Batch ID\",\"Transactiereferentie\",\"Machtigingskenmerk\",\"Incassant ID\",\"Betalingskenmerk\",\"Omschrijving-1\",\"Omschrijving-2\",\"Omschrijving-3\",\"Reden retour\",\"Oorspr bedrag\",\"Oorspr munt\",\"Koers\"";

    [TestMethod]
    public void Parse_WhenOnlyHeader_ThenReturnsEmptyList()
    {
        using var stream = Stream(Header);

        var result = RabobankCsvImporter.Parse(stream, "user-1");

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void Parse_WhenSingleRow_ThenParsesAllFields()
    {
        var row = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000011111\",\"2026-04-15\",\"2026-04-15\",\"-24,66\",\"+5555,27\",\"THEIRS01\",\"Shop\",\"\",\"\",\"RABONLLLXXX\",\"ie\",\"0000000000000000\",\"\",\"\",\"\",\"\",\"Payment\",\" \",\"\",\"\",\"\",\"\",\"\"";
        using var stream = Stream(Header + "\n" + row);

        var result = RabobankCsvImporter.Parse(stream, "user-1");

        var transaction = result.Single();
        Assert.AreEqual("OWNED1", transaction.Iban);
        Assert.AreEqual("EUR", transaction.Currency);
        Assert.AreEqual(new DateOnly(2026, 4, 15), transaction.Date);
        Assert.AreEqual(-24.66m, transaction.Amount);
        Assert.AreEqual("THEIRS01", transaction.IbanOtherParty);
        Assert.AreEqual("Shop", transaction.NameOtherParty);
        Assert.AreEqual("ie", transaction.Code);
        Assert.AreEqual("Payment", transaction.Description);
        Assert.AreEqual(11111, transaction.FollowNumber);
        Assert.AreEqual("user-1", transaction.UserId);
    }

    [TestMethod]
    public void Parse_WhenMultipleRows_ThenParsesEach()
    {
        var row1 = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000000001\",\"2026-01-03\",\"2026-01-03\",\"-150,00\",\"\",\"THEIRS1\",\"Albert Heijn\",\"\",\"\",\"\",\"bc\",\"\",\"\",\"\",\"\",\"\",\"Groceries\",\"\",\"\",\"\",\"\",\"\",\"\"";
        var row2 = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000000002\",\"2026-01-05\",\"2026-01-05\",\"3000,00\",\"\",\"THEIRS2\",\"Employer\",\"\",\"\",\"\",\"sb\",\"\",\"\",\"\",\"\",\"\",\"Salary\",\"\",\"\",\"\",\"\",\"\",\"\"";
        using var stream = Stream(Header + "\n" + row1 + "\n" + row2);

        var result = RabobankCsvImporter.Parse(stream, "user-1");

        Assert.HasCount(2, result);
        Assert.AreEqual(-150m, result[0].Amount);
        Assert.AreEqual(3000m, result[1].Amount);
    }

    [TestMethod]
    public void Parse_WhenDescriptionConcatenated_ThenTrimmed()
    {
        var row = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000000001\",\"2026-01-03\",\"2026-01-03\",\"-150,00\",\"\",\"THEIRS1\",\"Shop\",\"\",\"\",\"\",\"bc\",\"\",\"\",\"\",\"\",\"\",\"A\",\" \",\"B\",\"\",\"\",\"\",\"\"";
        using var stream = Stream(Header + "\n" + row);

        var result = RabobankCsvImporter.Parse(stream, "user-1");

        Assert.AreEqual("A B", result.Single().Description);
    }

    [TestMethod]
    public void Parse_WhenQuotedFieldContainsComma_ThenParsedAsSingleField()
    {
        var row = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000000001\",\"2026-01-03\",\"2026-01-03\",\"-150,00\",\"\",\"THEIRS1\",\"Janssen, B.V.\",\"\",\"\",\"\",\"bc\",\"\",\"\",\"\",\"\",\"\",\"Shop\",\"\",\"\",\"\",\"\",\"\",\"\"";
        using var stream = Stream(Header + "\n" + row);

        var result = RabobankCsvImporter.Parse(stream, "user-1");

        Assert.AreEqual("Janssen, B.V.", result.Single().NameOtherParty);
    }

    [TestMethod]
    public async Task ProcessAsync_WhenDuplicateRows_ThenOnlyNewRowsInserted()
    {
        var userId = "user-1";
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new BudgetDbContext(options);
        var importer = new RabobankCsvImporter(db);

        var row = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000000001\",\"2026-01-03\",\"2026-01-03\",\"-150,00\",\"\",\"THEIRS1\",\"Albert Heijn\",\"\",\"\",\"\",\"bc\",\"\",\"\",\"\",\"\",\"\",\"Groceries\",\"\",\"\",\"\",\"\",\"\",\"\"";
        using var stream = Stream(Header + "\n" + row);

        var firstMaxDate = await importer.ProcessAsync(stream, userId, CancellationToken.None);

        await using var secondStream = Stream(Header + "\n" + row);
        var secondMaxDate = await importer.ProcessAsync(secondStream, userId, CancellationToken.None);

        Assert.AreEqual(new DateOnly(2026, 1, 3), firstMaxDate);
        Assert.AreEqual(new DateOnly(2026, 1, 3), secondMaxDate);
        var stored = await db.Transactions.Where(t => t.UserId == userId).ToListAsync();
        Assert.HasCount(1, stored, "Duplicate upload should not create duplicate transactions");
    }

    [TestMethod]
    public async Task ProcessAsync_WhenMultipleRows_ThenReturnsMaxDate()
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new BudgetDbContext(options);
        var importer = new RabobankCsvImporter(db);

        var row1 = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000000001\",\"2026-01-03\",\"2026-01-03\",\"-150,00\",\"\",\"THEIRS1\",\"Albert Heijn\",\"\",\"\",\"\",\"bc\",\"\",\"\",\"\",\"\",\"\",\"Groceries\",\"\",\"\",\"\",\"\",\"\",\"\"";
        var row2 = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000000002\",\"2026-02-05\",\"2026-02-05\",\"3000,00\",\"\",\"THEIRS2\",\"Employer\",\"\",\"\",\"\",\"sb\",\"\",\"\",\"\",\"\",\"\",\"Salary\",\"\",\"\",\"\",\"\",\"\",\"\"";
        using var stream = Stream(Header + "\n" + row1 + "\n" + row2);

        var maxDate = await importer.ProcessAsync(stream, "user-1", CancellationToken.None);

        Assert.AreEqual(new DateOnly(2026, 2, 5), maxDate);
    }

    [TestMethod]
    public async Task ProcessAsync_WhenOnlyHeader_ThenReturnsMinDateAndInsertsNothing()
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new BudgetDbContext(options);
        var importer = new RabobankCsvImporter(db);

        using var stream = Stream(Header);
        var maxDate = await importer.ProcessAsync(stream, "user-1", CancellationToken.None);

        Assert.AreEqual(DateOnly.MinValue, maxDate);
        Assert.IsEmpty(await db.Transactions.ToListAsync());
    }

    [TestMethod]
    public void Parse_WhenMalformedDate_ThenThrows()
    {
        var row = "\"OWNED1\",\"EUR\",\"RABONL2U\",\"000000000000000001\",\"not-a-date\",\"2026-01-03\",\"-150,00\",\"\",\"THEIRS1\",\"Shop\",\"\",\"\",\"\",\"bc\",\"\",\"\",\"\",\"\",\"\",\"Groceries\",\"\",\"\",\"\",\"\",\"\",\"\"";
        using var stream = Stream(Header + "\n" + row);

        Assert.ThrowsExactly<FormatException>(() => RabobankCsvImporter.Parse(stream, "user-1"));
    }

    private static MemoryStream Stream(string csv)
        => new(Encoding.Latin1.GetBytes(csv));
}
