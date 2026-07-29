using System.Globalization;
using System.Text;

namespace Budget.Playwright.Support;

public class CsvBuilder
{
    private static readonly string[] Headers =
    [
        "IBAN/BBAN", "Munt", "BIC", "Volgnr", "Datum", "Rentedatum", "Bedrag",
        "Saldo na trn", "Tegenrekening IBAN/BBAN", "Naam tegenpartij",
        "Naam uiteindelijke partij", "Naam initiërende partij", "BIC tegenpartij",
        "Code", "Batch ID", "Transactiereferentie", "Machtigingskenmerk",
        "Incassant ID", "Betalingskenmerk", "Omschrijving-1", "Omschrijving-2",
        "Omschrijving-3", "Reden retour", "Oorspr bedrag", "Oorspr munt", "Koers"
    ];

    private readonly List<TestTransaction> _transactions = [];

    public CsvBuilder Add(TestTransaction transaction)
    {
        _transactions.Add(transaction);
        return this;
    }

    public string Build()
    {
        var lines = new List<string>
        {
            string.Join(",", Headers.Select(h => $"\"{h}\""))
        };

        foreach (var t in _transactions)
        {
            var amount = t.Amount.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
            var paddedFn = t.FollowNumber.ToString("D18");

            var fields = new[]
            {
                Q(t.Iban),
                Q(t.Currency),
                Q("RABONL2U"),
                Q(paddedFn),
                Q(t.Date.ToString("yyyy-MM-dd")),
                Q(t.Date.ToString("yyyy-MM-dd")),
                Q(amount),
                Q(""),
                Q(t.IbanOtherParty),
                Q(t.NameOtherParty),
                Q(""),
                Q(""),
                Q(""),
                Q(t.Code),
                Q("0000000000000000"),
                Q(""),
                Q(""),
                Q(""),
                Q(""),
                Q(t.Description),
                Q(" "),
                Q(""),
                Q(""),
                Q(""),
                Q(""),
                Q(""),
            };

            lines.Add(string.Join(",", fields));
        }

        return string.Join("\n", lines);
    }

    public byte[] BuildBytes()
    {
        return Encoding.Latin1.GetBytes(Build());
    }

    private static string Q(string value) => $"\"{value}\"";
}
