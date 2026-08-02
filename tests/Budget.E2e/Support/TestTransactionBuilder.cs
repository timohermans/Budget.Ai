namespace Budget.E2e.Support;

public class TestTransactionBuilder
{
    private DateOnly _date;
    private decimal _amount;
    private string _code = "bc";
    private string _currency = "EUR";
    private string _description = "";
    private int _followNumber = 1;
    private string _iban = "NL12RABO0123456789";
    private string _ibanOtherParty = "NL98INGB9876543210";
    private string _nameOtherParty = "Test Party";

    public TestTransactionBuilder On(DateOnly date) { _date = date; return this; }
    public TestTransactionBuilder Amount(decimal amount) { _amount = amount; return this; }
    public TestTransactionBuilder Code(string code) { _code = code; return this; }
    public TestTransactionBuilder Named(string name) { _nameOtherParty = name; return this; }
    public TestTransactionBuilder DescribedAs(string description) { _description = description; return this; }
    public TestTransactionBuilder Iban(string iban) { _iban = iban; return this; }
    public TestTransactionBuilder IbanOtherParty(string iban) { _ibanOtherParty = iban; return this; }
    public TestTransactionBuilder FollowNumber(int number) { _followNumber = number; return this; }
    public TestTransactionBuilder Currency(string currency) { _currency = currency; return this; }

    public TestTransaction Build() => new()
    {
        Date = _date,
        Amount = _amount,
        Code = _code,
        Currency = _currency,
        Description = _description,
        FollowNumber = _followNumber,
        Iban = _iban,
        IbanOtherParty = _ibanOtherParty,
        NameOtherParty = _nameOtherParty,
    };

    public static implicit operator TestTransaction(TestTransactionBuilder builder) => builder.Build();
}
