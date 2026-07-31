namespace Budget.Web.Domain.Transactions;

public static class TransactionClassifier
{
    private static readonly string[] FixedCodes = ["sb", "cb", "bg", "ei", "tb"];

    /// <summary>Classifies a transaction as fixed by applying the fixed/variable rules in precedence order.</summary>
    /// <param name="transaction">The transaction to classify.</param>
    /// <param name="myIbans">The user's own IBANs, used to detect transfers between own accounts.</param>
    public static bool IsFixed(Transaction transaction, IReadOnlyCollection<string> myIbans)
    {
        if (transaction.IsNotFixed)
            return false;
        if (transaction.IsIncome() && transaction.IsFromOwnAccount(myIbans))
            return false;
        if (transaction.NameOtherParty.Contains("paypal", StringComparison.OrdinalIgnoreCase))
            return false;
        if (transaction.Code == "db"
            && transaction.Description is not null
            && transaction.Description.Contains("sparen", StringComparison.OrdinalIgnoreCase))
            return true;
        if (transaction.Code == "db" && transaction.NameOtherParty == "Rabobank")
            return true;
        if (FixedCodes.Contains(transaction.Code))
            return true;
        return false;
    }

    /// <summary>Classifies a transaction as variable, i.e. the inverse of <see cref="IsFixed"/>.</summary>
    /// <param name="transaction">The transaction to classify.</param>
    /// <param name="myIbans">The user's own IBANs, used to detect transfers between own accounts.</param>
    public static bool IsVariable(Transaction transaction, IReadOnlyCollection<string> myIbans)
        => !IsFixed(transaction, myIbans);
}
