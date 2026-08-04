namespace Budget.Web.Domain.Transactions;

public sealed class WeekSummary
{
    public required int WeekNumber { get; init; }
    public decimal Budget { get; set; }
    public decimal Spent { get; set; }
    public decimal Left { get; set; }
    public List<TransactionTemplateModel> Transactions { get; } = [];
}

public sealed class BalanceSummary
{
    public required string Iban { get; init; }
    public decimal Balance { get; set; }
    public List<TransactionTemplateModel> Transactions { get; } = [];
}

public sealed class Summary
{
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Spent { get; set; }
    public decimal Left { get; set; }
    public decimal Budget { get; set; }
    public Dictionary<int, WeekSummary> Weeks { get; } = [];
    public List<TransactionTemplateModel> IncomeTransactions { get; } = [];
    public List<TransactionTemplateModel> ExpenseTransactions { get; } = [];
    public Dictionary<string, BalanceSummary> IbanBalances { get; } = [];
}

public sealed record TransactionTemplateModel(
    int Id,
    decimal Amount,
    DateOnly Date,
    bool IsFixed,
    bool IsNotFixed,
    string NameOtherParty,
    string? Description,
    string? LogoUrl = null,
    string? DisplayName = null);
