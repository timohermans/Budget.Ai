using Budget.Web.Domain;
using Budget.Web.Domain.Transactions;

namespace Budget.Tests.Domain;

[TestClass]
public class SummaryCalculatorTests
{
    private const int Year = 2099;
    private const int Month = 1;
    private static readonly DateOnly FirstDay = new(Year, Month, 1);
    private static readonly DateOnly LastMonth = FirstDay.AddMonths(-1);
    private const string MainIban = "NL12RABO0123456789";
    private const string SavingsIban = "NL12RABO0987654321";
    private const string OtherIban = "NL98INGB9876543210";

    [TestMethod]
    public void Calculate_WhenLastMonthHasFixedIncomeAndExpensesAndThisMonthVariableExpenses_ThenComputesBaseline()
    {
        var transactions = new[]
        {
            Transaction(1, 3000m, LastMonth.AddDays(14), "sb", "Employer", MainIban, OtherIban),
            Transaction(2, -800m, LastMonth.AddDays(14), "cb", "Insurance Co", MainIban, OtherIban),
            Transaction(3, -150m, FirstDay.AddDays(2), "bc", "Albert Heijn", MainIban, OtherIban),
        };

        var summary = Calculate(transactions);

        Assert.AreEqual(3000m, summary.Income);
        Assert.AreEqual(800m, summary.Expenses);
        Assert.AreEqual(2200m, summary.Budget);
        Assert.AreEqual(150m, summary.Spent);
        Assert.AreEqual(2050m, summary.Left);
    }

    [TestMethod]
    public void Calculate_WhenVariableExpenses_ThenSpentOnlyIncludesVariable()
    {
        var transactions = new[]
        {
            Transaction(1, 3000m, LastMonth.AddDays(14), "sb", "Employer", MainIban, OtherIban),
            Transaction(2, -800m, LastMonth.AddDays(14), "cb", "Insurance Co", MainIban, OtherIban),
            Transaction(3, -150m, FirstDay.AddDays(2), "bc", "Albert Heijn", MainIban, OtherIban),
            Transaction(4, -50m, FirstDay.AddDays(4), "bc", "Hema", MainIban, OtherIban),
            Transaction(5, -100m, FirstDay.AddDays(9), "ie", "Bol.com", MainIban, OtherIban),
        };

        var summary = Calculate(transactions);

        Assert.AreEqual(300m, summary.Spent);
    }

    [TestMethod]
    public void Calculate_WhenNoIbans_ThenReturnsEmptyZeroSummary()
    {
        var summary = SummaryCalculator.Calculate(Year, Month, null, [], []);

        Assert.AreEqual(0m, summary.Income);
        Assert.AreEqual(0m, summary.Expenses);
        Assert.AreEqual(0m, summary.Spent);
        Assert.AreEqual(0m, summary.Left);
        Assert.AreEqual(0m, summary.Budget);
        Assert.IsEmpty(summary.Weeks);
        Assert.IsEmpty(summary.IbanBalances);
    }

    [TestMethod]
    public void Calculate_WhenWeekBudgetDistributed_ThenProportionalToDays()
    {
        var transactions = new[]
        {
            Transaction(1, 3000m, LastMonth.AddDays(14), "sb", "Employer", MainIban, OtherIban),
            Transaction(2, -800m, LastMonth.AddDays(14), "cb", "Insurance Co", MainIban, OtherIban),
            Transaction(3, -150m, FirstDay.AddDays(2), "bc", "Albert Heijn", MainIban, OtherIban),
        };

        var summary = Calculate(transactions);

        Assert.AreEqual(283.87m, Rounded2(summary.Weeks[1].Budget), "Week 1 (4 days) should get 4/31 of the budget");
        Assert.AreEqual(496.77m, Rounded2(summary.Weeks[2].Budget), "Week 2 (7 days) should get 7/31 of the budget");
        Assert.AreEqual(496.77m, Rounded2(summary.Weeks[3].Budget), "Week 3 (7 days) should get 7/31 of the budget");
        Assert.AreEqual(496.77m, Rounded2(summary.Weeks[4].Budget), "Week 4 (7 days) should get 7/31 of the budget");
        Assert.AreEqual(425.81m, Rounded2(summary.Weeks[5].Budget), "Week 5 (6 days) should get 6/31 of the budget");
    }

    [TestMethod]
    public void Calculate_WhenFixedIncomeFromLastMonth_ThenCounted()
    {
        var transactions = new[]
        {
            Transaction(1, 2000m, LastMonth.AddDays(0), "sb", "Employer", MainIban, OtherIban),
            Transaction(2, 1000m, LastMonth.AddDays(9), "sb", "Freelance Client", MainIban, OtherIban),
            Transaction(3, 500m, LastMonth.AddDays(19), "sb", "Side Gig", MainIban, OtherIban),
        };

        var summary = Calculate(transactions);

        Assert.AreEqual(3500m, summary.Income);
    }

    [TestMethod]
    public void Calculate_WhenFixedExpensesFromLastMonth_ThenCounted()
    {
        var transactions = new[]
        {
            Transaction(1, 3000m, LastMonth.AddDays(9), "sb", "Employer", MainIban, OtherIban),
            Transaction(2, -800m, LastMonth.AddDays(11), "cb", "Insurance", MainIban, OtherIban),
            Transaction(3, -400m, LastMonth.AddDays(17), "cb", "Mortgage", MainIban, OtherIban),
            Transaction(4, -300m, LastMonth.AddDays(24), "cb", "Netflix", MainIban, OtherIban),
            Transaction(5, -150m, LastMonth.AddDays(4), "bc", "Albert Heijn", MainIban, OtherIban),
            Transaction(6, -75m, LastMonth.AddDays(21), "bc", "Jumbo", MainIban, OtherIban),
        };

        var summary = Calculate(transactions);

        Assert.AreEqual(1500m, summary.Expenses);
    }

    [TestMethod]
    public void Calculate_WhenOwnAccountTransferInLastMonth_ThenExcludedFromIncome()
    {
        var transactions = new[]
        {
            Transaction(1, 3000m, LastMonth.AddDays(14), "sb", "Employer", MainIban, OtherIban),
            Transaction(2, 500m, LastMonth.AddDays(15), "db", "Rabobank", MainIban, SavingsIban),
            Transaction(3, -1000m, LastMonth.AddDays(16), "cb", "Insurance Co", SavingsIban, OtherIban),
        };

        var summary = Calculate(transactions);

        Assert.AreEqual(3000m, summary.Income, "Transfer in from own savings account should not count as income");
        Assert.AreEqual(3000m, summary.Budget, "Budget should be unaffected by the own-account transfer in");
    }

    [TestMethod]
    public void Calculate_WhenPayPalTransactionsLastMonth_ThenExcluded()
    {
        var transactions = new[]
        {
            Transaction(1, 3000m, LastMonth.AddDays(14), "sb", "Employer", MainIban, OtherIban),
            Transaction(2, 500m, LastMonth.AddDays(14), "sb", "PayPal EU", MainIban, OtherIban),
            Transaction(3, -300m, LastMonth.AddDays(14), "cb", "PayPal EU", MainIban, OtherIban),
        };

        var summary = Calculate(transactions);

        Assert.AreEqual(3000m, summary.Income, "PayPal income should be excluded from fixed income");
        Assert.AreEqual(0m, summary.Expenses, "PayPal expenses should be excluded from fixed expenses");
    }

    [TestMethod]
    public void Calculate_WhenMultipleIbansThisMonth_ThenBalancesComputed()
    {
        var transactions = new[]
        {
            Transaction(1, 3000m, LastMonth.AddDays(14), "sb", "Employer", MainIban, OtherIban),
            Transaction(2, -800m, LastMonth.AddDays(14), "cb", "Insurance Co", MainIban, OtherIban),
            Transaction(3, -150m, FirstDay.AddDays(2), "bc", "Albert Heijn", MainIban, OtherIban),
            Transaction(4, 2000m, FirstDay.AddDays(11), "sb", "Freelance Client", OtherIban, MainIban),
        };

        var summary = Calculate(transactions);

        Assert.HasCount(2, summary.IbanBalances);
        Assert.AreEqual(-150m, summary.IbanBalances[MainIban].Balance, "Main account balance should be net of this month's transactions");
        Assert.AreEqual(2000m, summary.IbanBalances[OtherIban].Balance, "Second account balance should be net of this month's transactions");
    }

    [TestMethod]
    public void Calculate_WhenExplicitIbanSelected_ThenUsesThatIban()
    {
        var transactions = new[]
        {
            Transaction(1, 3000m, LastMonth.AddDays(14), "sb", "Employer", MainIban, "NL99EXTERNAL1"),
            Transaction(2, 1000m, LastMonth.AddDays(14), "sb", "Client", OtherIban, "NL99EXTERNAL2"),
            Transaction(3, -200m, FirstDay.AddDays(2), "bc", "Shop", OtherIban, "NL99EXTERNAL3"),
        };

        var summary = Calculate(transactions, selectedIban: OtherIban);

        Assert.AreEqual(1000m, summary.Income, "Income should come from the selected account");
        Assert.AreEqual(200m, summary.Spent, "Spent should come from the selected account");
    }

    [TestMethod]
    public void Calculate_WhenIbanNotInOwnIbans_ThenThrows()
    {
        var transactions = new[] { Transaction(1, -150m, FirstDay.AddDays(2), "bc", "Albert Heijn", MainIban, OtherIban) };

        Assert.ThrowsExactly<DomainError>(() => Calculate(transactions, selectedIban: "NL99UNKNOWN0000000"));
    }

    [TestMethod]
    public void Calculate_WhenComplexData_ThenMatchesKnownTotals()
    {
        var transactions = new[]
        {
            Transaction(1, 3000.3m, new DateOnly(2025, 12, 12), "sb", "Werkgever A", "OWNED01", "WORK01"),
            Transaction(2, 2000.3m, new DateOnly(2025, 12, 12), "sb", "Werkgever B", "OWNED01", "WORK02"),
            Transaction(3, -44m, new DateOnly(2025, 12, 8), "bg", "Piano lerares", "OWNED01", "HOBBY01"),
            Transaction(4, -51.03m, new DateOnly(2025, 12, 6), "ei", "ODIDO Netherlands", "OWNED01", "CORPO01"),
            Transaction(5, -109m, new DateOnly(2025, 12, 6), "cb", "ESSENT RETAIL ENERGIE B.V.", "OWNED01", "CORPO02"),
            Transaction(6, -5.45m, new DateOnly(2025, 12, 2), "db", "Rabobank", "OWNED01", "CORPO03"),
            Transaction(7, -1801.81m, new DateOnly(2025, 12, 28), "ei", "BLG Wonen", "OWNED01", "CORPO04"),
            Transaction(8, -20.72m, new DateOnly(2026, 1, 2), "bc", "AH - Jan Linders 4141", "OWNED01", "SHOP01"),
            Transaction(9, -300m, new DateOnly(2026, 1, 2), "bc", "AH - Jan Linders 4141", "OWNED01", "SHOP01"),
            Transaction(10, -800m, new DateOnly(2026, 1, 11), "bc", "AH - Jan Linders 4141", "OWNED01", "SHOP01"),
            Transaction(11, -1000m, new DateOnly(2026, 1, 11), "tb", "Spaar", "OWNED01", "OWNED02"),
            Transaction(12, 1000m, new DateOnly(2026, 1, 11), "bc", "Betaalrekening", "OWNED02", "OWNED01"),
            Transaction(13, 500m, new DateOnly(2026, 1, 11), "tb", "Spaar", "OWNED01", "OWNED02"),
            Transaction(14, -500m, new DateOnly(2026, 1, 11), "bc", "Betaalrekening", "OWNED02", "OWNED01"),
        };

        var summary = SummaryCalculator.Calculate(
            2026, 1, null,
            transactions,
            ["OWNED01", "OWNED02"]);

        Assert.AreEqual(5000.6m, summary.Income);
        Assert.AreEqual(2011.29m, summary.Expenses);
        Assert.AreEqual(620.72m, summary.Spent, "Variable income this month reduces spent");
        Assert.AreEqual(2989.31m, summary.Budget);
        Assert.AreEqual(2368.59m, summary.Left);
    }

    private static Summary Calculate(IReadOnlyCollection<Transaction> transactions, string? selectedIban = null)
    {
        var ownIbans = transactions
            .GroupBy(t => t.Iban)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        return SummaryCalculator.Calculate(Year, Month, selectedIban, transactions, ownIbans);
    }

    private static Transaction Transaction(
        int followNumber,
        decimal amount,
        DateOnly date,
        string code,
        string nameOtherParty,
        string iban,
        string ibanOtherParty)
        => new()
        {
            FollowNumber = followNumber,
            Amount = amount,
            Date = date,
            Code = code,
            NameOtherParty = nameOtherParty,
            Iban = iban,
            IbanOtherParty = ibanOtherParty,
        };

    private static decimal Rounded2(decimal value) => Math.Round(value, 2);
}
