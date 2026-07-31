using System.Globalization;

namespace Budget.Web.Domain.Transactions;

public static class SummaryCalculator
{
    /// <summary>
    /// Computes the monthly budget summary for the given year and month from the user's transactions.
    /// </summary>
    /// <param name="year">The year of the month to summarize.</param>
    /// <param name="month">The month to summarize.</param>
    /// <param name="iban">The IBAN to treat as the main account, or <see langword="null"/> to use the most frequently used one.</param>
    /// <param name="transactions">The user's transactions; only those within the month window are considered.</param>
    /// <param name="ownIbans">The user's own IBANs ordered by transaction count descending.</param>
    /// <returns>A summary of the month's budget, spending, weeks, and IBAN balances.</returns>
    /// <exception cref="DomainError">Thrown when <paramref name="iban"/> is not one of <paramref name="ownIbans"/>.</exception>
    public static Summary Calculate(
        int year,
        int month,
        string? iban,
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyList<string> ownIbans)
    {
        var thisMonth = new DateOnly(year, month, 1);
        var lastMonth = thisMonth.AddMonths(-1);
        var nextMonth = thisMonth.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var window = transactions
            .Where(t => t.Date >= lastMonth && t.Date < nextMonth)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.NameOtherParty)
            .ToList();

        if (ownIbans.Count == 0)
            return new Summary();

        var targetIban = iban ?? ownIbans[0];
        if (!ownIbans.Contains(targetIban))
            throw new DomainError("Iban does not exist.");

        var datesPerWeek = new Dictionary<int, int>();
        var weeks = new Dictionary<int, WeekSummary>();
        for (var day = 1; day <= daysInMonth; day++)
        {
            var dateInMonth = new DateOnly(year, month, day);
            var week = ISOWeek.GetWeekOfYear(dateInMonth);
            datesPerWeek[week] = datesPerWeek.GetValueOrDefault(week) + 1;
            weeks.TryAdd(week, new WeekSummary { WeekNumber = week });
        }

        var summary = new Summary();
        foreach (var week in weeks.Values)
            summary.Weeks[week.WeekNumber] = week;

        foreach (var transaction in window)
        {
            var template = new TransactionTemplateModel(
                transaction.Id,
                transaction.Amount,
                transaction.Date,
                TransactionClassifier.IsFixed(transaction, ownIbans),
                transaction.IsNotFixed,
                transaction.NameOtherParty,
                transaction.Description);

            var isLastMonth = transaction.Date.Year == lastMonth.Year
                && transaction.Date.Month == lastMonth.Month;
            var isThisMonth = !isLastMonth;
            var week = ISOWeek.GetWeekOfYear(transaction.Date);
            var isTargetIban = targetIban == transaction.Iban;

            if (isTargetIban && isLastMonth && template.IsFixed && transaction.IsIncome())
            {
                summary.Income += transaction.Amount;
                summary.IncomeTransactions.Add(template);
            }

            if (isTargetIban && isLastMonth && template.IsFixed && transaction.IsExpense())
            {
                summary.Expenses += Math.Abs(transaction.Amount);
                summary.ExpenseTransactions.Add(template);
            }

            if (isTargetIban && isThisMonth)
            {
                var weekSummary = summary.Weeks[week];
                if (TransactionClassifier.IsVariable(transaction, ownIbans))
                {
                    summary.Spent += transaction.Amount * -1;
                    weekSummary.Spent += transaction.Amount * -1;
                }
                weekSummary.Transactions.Add(template);
            }

            if (isThisMonth)
            {
                if (!summary.IbanBalances.TryGetValue(transaction.Iban, out var balance))
                {
                    balance = new BalanceSummary { Iban = transaction.Iban };
                    summary.IbanBalances[transaction.Iban] = balance;
                }
                balance.Balance += transaction.Amount;
                balance.Transactions.Add(template);
            }
        }

        summary.Budget = Math.Abs(summary.Income) - Math.Abs(summary.Expenses);
        summary.Left = summary.Budget - summary.Spent;

        foreach (var week in summary.Weeks.Values)
        {
            week.Budget = summary.Budget / daysInMonth * datesPerWeek[week.WeekNumber];
            week.Left = Math.Abs(week.Budget) - Math.Abs(week.Spent);
        }

        return summary;
    }
}
