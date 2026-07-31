using Budget.Web.Domain.Transactions;

namespace Budget.Tests.Domain;

[TestClass]
public class TransactionClassifierTests
{
    private static readonly string[] MyIbans = ["NL12RABO0123456789", "NL12RABO0987654321"];

    [TestMethod]
    public void IsFixed_WhenTransactionIsFlaggedVariable_ThenReturnsFalse()
    {
        var transaction = Transaction(amount: -100m, code: "cb", isNotFixed: true);

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsFixed_WhenIncomeFromOwnAccount_ThenReturnsFalse()
    {
        var transaction = Transaction(amount: 500m, code: "sb", nameOtherParty: "Rabobank", ibanOtherParty: "NL12RABO0987654321");

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsFixed_WhenExpenseFromOwnAccount_ThenReturnsTrue()
    {
        var transaction = Transaction(amount: -500m, code: "tb", nameOtherParty: "Spaar", ibanOtherParty: "NL12RABO0987654321");

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsFixed_WhenCounterpartyNameContainsPayPal_ThenReturnsFalse()
    {
        var transaction = Transaction(amount: -100m, code: "cb", nameOtherParty: "PayPal EU");

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsFixed_WhenCodeDbAndDescriptionContainsSparen_ThenReturnsTrue()
    {
        var transaction = Transaction(amount: -1000m, code: "db", nameOtherParty: "Spaar", description: "Maandelijks sparen");

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsFixed_WhenCodeDbAndCounterpartyIsRabobank_ThenReturnsTrue()
    {
        var transaction = Transaction(amount: -5.45m, code: "db", nameOtherParty: "Rabobank", description: "Kosten basispakket");

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("sb")]
    [DataRow("cb")]
    [DataRow("bg")]
    [DataRow("ei")]
    [DataRow("tb")]
    public void IsFixed_WhenCodeIsFixedCode_ThenReturnsTrue(string code)
    {
        var transaction = Transaction(amount: -100m, code: code);

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsFixed_WhenNoRuleMatches_ThenReturnsFalse()
    {
        var transaction = Transaction(amount: -150m, code: "bc", nameOtherParty: "Albert Heijn");

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsFixed_WhenFlaggedVariableOverridesFixedCode_ThenReturnsFalse()
    {
        var transaction = Transaction(amount: -800m, code: "cb", nameOtherParty: "Insurance Co", isNotFixed: true);

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsFixed_WhenIncomeFromOwnAccountOverridesFixedCode_ThenReturnsFalse()
    {
        var transaction = Transaction(amount: 500m, code: "sb", nameOtherParty: "Rabobank", ibanOtherParty: "NL12RABO0987654321");

        var result = TransactionClassifier.IsFixed(transaction, MyIbans);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsVariable_WhenTransactionIsFixed_ThenReturnsFalse()
    {
        var transaction = Transaction(amount: -100m, code: "cb");

        var result = TransactionClassifier.IsVariable(transaction, MyIbans);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsVariable_WhenTransactionIsVariable_ThenReturnsTrue()
    {
        var transaction = Transaction(amount: -100m, code: "bc");

        var result = TransactionClassifier.IsVariable(transaction, MyIbans);

        Assert.IsTrue(result);
    }

    private static Transaction Transaction(
        decimal amount,
        string code,
        string nameOtherParty = "Test Party",
        string ibanOtherParty = "NL98INGB9876543210",
        bool isNotFixed = false,
        string? description = null)
        => new()
        {
            Amount = amount,
            Code = code,
            NameOtherParty = nameOtherParty,
            IbanOtherParty = ibanOtherParty,
            IsNotFixed = isNotFixed,
            Description = description,
        };
}
