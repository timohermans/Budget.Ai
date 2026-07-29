namespace Budget.Playwright.Support;

public record TestTransaction
{
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Description { get; init; } = "";
    public string Description2 { get; init; } = "";
    public string Description3 { get; init; } = "";
    public int FollowNumber { get; init; }
    public string Code { get; init; } = "bc";
    public string Iban { get; init; } = "NL12RABO0123456789";
    public string IbanOtherParty { get; init; } = "NL98INGB9876543210";
    public string NameOtherParty { get; init; } = "Test Party";
}
