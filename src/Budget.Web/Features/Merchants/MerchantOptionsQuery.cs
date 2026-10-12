using Budget.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Features.Merchants;

/// <summary>Shared query for the merchant link-picker options.</summary>
public static class MerchantOptionsQuery
{
    public static async Task<List<MerchantOptionModel>> GetOptionsAsync(
        BudgetDbContext db, string? q, CancellationToken ct)
    {
        var query = q?.Trim().ToLowerInvariant();

        var merchants = await db
            .Merchants
            .Select(m => new
            {
                m.NameNormalized,
                m.DisplayName,
                m.UpdatedAt,
                LastLinked = m.Aliases.Select(a => (DateTimeOffset?)a.CreatedAt).Max(),
                Aliases = m.Aliases.Select(a => a.NameNormalized)
            })
            .Where(m => query == null || m.DisplayName.ToLower().Contains(query))
            .ToListAsync();
        
        var transactionsByName = await db
            .Transactions
            .Select(t => t.NameOtherPartyNormalized)
            .ToListAsync();

        var result = merchants.Select(m => new MerchantOptionModel
        {
            NameNormalized = m.NameNormalized,
            Label = m.DisplayName,
            LastLinked = m.UpdatedAt > m.LastLinked.GetValueOrDefault(DateTimeOffset.MinValue) ? m.UpdatedAt : m.LastLinked,
            TotalTransactions = transactionsByName.Count(t => t == m.NameNormalized || m.Aliases.Contains(t)),
        })
        .OrderByDescending(o => o.LastLinked)
        .ThenBy(o => o.NameNormalized)
        .Take(5)
        .ToList();

        return result;
    }
}
