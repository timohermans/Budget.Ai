namespace Budget.Web.Domain.Transactions;

public class Transaction
{
    public int Id { get; set; }
    public int FollowNumber { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string? Currency { get; set; } = "EUR";
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string NameOtherParty { get; set; } = string.Empty;
    public string IbanOtherParty { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsNotFixed { get; set; }
    public string Code { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    /// <summary>Determines whether the transaction is an expense, i.e. its amount is negative.</summary>
    public bool IsExpense() => Amount < 0;

    /// <summary>Determines whether the transaction is income, i.e. its amount is zero or positive.</summary>
    public bool IsIncome() => !IsExpense();

    /// <summary>Determines whether the counterparty account is one of the user's own accounts.</summary>
    /// <param name="myIbans">The user's own IBANs.</param>
    public bool IsFromOwnAccount(IReadOnlyCollection<string> myIbans) => myIbans.Contains(IbanOtherParty);
}
