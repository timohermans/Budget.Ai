using Budget.Web.Domain.Transactions;

public record TransactionOverviewQueryResult(Transaction Transaction, string? DisplayName, string? LogoUrl);
