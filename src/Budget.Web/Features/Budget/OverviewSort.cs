using Budget.Web.Domain.Transactions;

namespace Budget.Web.Features.Budget;

public enum OverviewSort
{
    DateDesc,
    DateAsc,
    AmountDescHoogsteUitgaves,
    AmountAscKleinsteUitgaves,
    NameAsc,
    NameDesc,
}

public static class OverviewSorts
{
    public static OverviewSort Parse(string? value) => value switch
    {
        "datum-begin-eind" => OverviewSort.DateAsc,
        "hoogste-uitgaves" => OverviewSort.AmountDescHoogsteUitgaves,
        "kleinste-uitgaves" => OverviewSort.AmountAscKleinsteUitgaves,
        "winkel-a-z" => OverviewSort.NameAsc,
        "winkel-z-a" => OverviewSort.NameDesc,
        _ => OverviewSort.DateDesc,
    };

    public static string? ToQuery(OverviewSort sort) => sort switch
    {
        OverviewSort.DateAsc => "datum-begin-eind",
        OverviewSort.AmountDescHoogsteUitgaves => "hoogste-uitgaves",
        OverviewSort.AmountAscKleinsteUitgaves => "kleinste-uitgaves",
        OverviewSort.NameAsc => "winkel-a-z",
        OverviewSort.NameDesc => "winkel-z-a",
        _ => null,
    };

    public static Comparison<TransactionTemplateModel> CreateComparison(OverviewSort sort) => sort switch
    {
        OverviewSort.DateAsc => (a, b) =>
        {
            var byDate = a.Date.CompareTo(b.Date);
            return byDate != 0 ? byDate : CompareNameDesc(a, b);
        },
        OverviewSort.AmountDescHoogsteUitgaves => (a, b) =>
        {
            var byAmount = a.Amount.CompareTo(b.Amount);
            return byAmount != 0 ? byAmount : CompareDateDesc(a, b);
        },
        OverviewSort.AmountAscKleinsteUitgaves => (a, b) =>
        {
            var byPivot = ExpensePivot(a).CompareTo(ExpensePivot(b));
            if (byPivot != 0) return byPivot;
            var bySize = Math.Abs(a.Amount).CompareTo(Math.Abs(b.Amount));
            return bySize != 0 ? bySize : CompareDateDesc(a, b);
        },
        OverviewSort.NameAsc => (a, b) =>
        {
            var byName = CompareNameIgnoreCase(a, b);
            return byName != 0 ? byName : CompareDateDesc(a, b);
        },
        OverviewSort.NameDesc => (a, b) =>
        {
            var byName = CompareNameIgnoreCase(b, a);
            return byName != 0 ? byName : CompareDateDesc(a, b);
        },
        _ => (a, b) =>
        {
            var byDate = CompareDateDesc(a, b);
            return byDate != 0 ? byDate : CompareNameDesc(a, b);
        },
    };

    private static int ExpensePivot(TransactionTemplateModel transaction) => transaction.Amount < 0 ? 0 : 1;

    private static int CompareDateDesc(TransactionTemplateModel a, TransactionTemplateModel b) =>
        b.Date.CompareTo(a.Date);

    private static int CompareNameDesc(TransactionTemplateModel a, TransactionTemplateModel b) =>
        Comparer<string>.Default.Compare(DisplayNameOf(b), DisplayNameOf(a));

    private static int CompareNameIgnoreCase(TransactionTemplateModel a, TransactionTemplateModel b) =>
        string.Compare(DisplayNameOf(a), DisplayNameOf(b), StringComparison.CurrentCultureIgnoreCase);

    private static string DisplayNameOf(TransactionTemplateModel transaction) =>
        transaction.DisplayName ?? transaction.NameOtherParty;
}