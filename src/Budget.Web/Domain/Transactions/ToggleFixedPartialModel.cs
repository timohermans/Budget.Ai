namespace Budget.Web.Domain.Transactions;

public sealed record ToggleFixedPartialModel(
    TransactionTemplateModel Transaction,
    int? WeekNumber = null,
    decimal? Spent = null,
    decimal? Left = null,
    decimal? WeekSpent = null,
    decimal? WeekLeft = null,
    decimal? WeekBudget = null);
