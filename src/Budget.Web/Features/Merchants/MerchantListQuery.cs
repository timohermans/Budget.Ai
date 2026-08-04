using Budget.Web.Data;
using Budget.Web.Domain.Merchants;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Features.Merchants;

/// <summary>Shared queries for the merchant admin page controllers: the rows list and the link-picker options.</summary>
public static class MerchantListQuery
{
    public static async Task<List<MerchantRowModel>> GetRowsAsync(
        BudgetDbContext db, string search, string sort, string dir, CancellationToken ct)
    {
        var transactionsQuery = 
        from t in db.Transactions
        join m in db.Merchants on t.NameOtherPartyNormalized equals m.NameNormalized into mj // this one is necessary when not linked manually but found naturally
        from m in mj.DefaultIfEmpty()
        join a in db.MerchantAliases on t.NameOtherPartyNormalized equals a.NameNormalized into aj
        from a in aj.DefaultIfEmpty()
        join ma in db.Merchants on a.MerchantId equals ma.Id into maj
        from ma in maj.DefaultIfEmpty()
        select new
        {
            Transaction = t,
            Merchant = m,
            MerchentAlias = ma,
        };
        var transactions = await transactionsQuery.ToListAsync();

        var rows = transactions
            .GroupBy(t => t.Transaction.NameOtherPartyNormalized)
                .Select(g =>
                {
                    var first = g.FirstOrDefault();
                    var m = first?.Merchant;
                    var ma = first?.MerchentAlias;
                    var t = first?.Transaction;

                    return new MerchantRowModel
                    {
                        NameNormalized = g.Key,
                        TransactionCount = g.Count(),
                        FirstSeen = g.Min(t => t.Transaction.Date),
                        RawName = t?.NameOtherParty ?? "",
                        Status = m?.Status ?? ma?.Status,
                        DisplayName = m?.DisplayName ?? ma?.DisplayName,
                        LogoUrl = m?.LogoUrl ?? ma?.LogoUrl,
                        LinkedToName = ma?.DisplayName
                    };
                })
            .OrderByDescending(g => g.TransactionCount)
            .ThenByDescending(g => g.NameNormalized)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLowerInvariant();
            rows = rows
                .Where(r => r.NameNormalized.Contains(q) || r.RawName.ToLowerInvariant().Contains(q))
                .ToList();
        }

        var isDesc = dir == "desc";
        rows = sort switch
        {
            "name" => isDesc
                ? rows.OrderByDescending(r => r.NameNormalized).ToList()
                : rows.OrderBy(r => r.NameNormalized).ToList(),
            "count" => isDesc
                ? rows.OrderByDescending(r => r.TransactionCount).ThenBy(r => r.NameNormalized).ToList()
                : rows.OrderBy(r => r.TransactionCount).ThenBy(r => r.NameNormalized).ToList(),
            "firstSeen" => isDesc
                ? rows.OrderByDescending(r => r.FirstSeen).ThenBy(r => r.NameNormalized).ToList()
                : rows.OrderBy(r => r.FirstSeen).ThenBy(r => r.NameNormalized).ToList(),
            "status" => isDesc
                ? rows.OrderByDescending(Rank).ThenBy(r => r.NameNormalized).ToList()
                : rows.OrderBy(Rank).ThenBy(r => r.NameNormalized).ToList(),
            _ => rows
                .OrderBy(Rank)
                .ThenBy(r => Rank(r) == 0 ? -r.TransactionCount : 0)
                .ThenBy(r => r.NameNormalized)
                .ToList(),
        };

        return rows;
    }

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
            .Where(m => query == null || m.NameNormalized.ToLowerInvariant().Contains(query))
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

    /// <summary>Builds the rows partial for the full page render and for the htmx swaps after each action.</summary>
    public static async Task<RowsPartialModel> BuildRowsPartialAsync(
        BudgetDbContext db, string search, string sort, string dir, CancellationToken ct)
    {
        var rows = await GetRowsAsync(db, search, sort, dir, ct);
        var recentOptions = await GetOptionsAsync(db, null, ct);
        return new RowsPartialModel
        {
            Search = search,
            Sort = sort,
            Dir = dir,
            Rows = rows,
            RecentOptions = recentOptions,
        };
    }

    private static int Rank(MerchantRowModel row)
        => row.IsLinked
            ? 1
            : row.Status switch
            {
                null => 0,
                MerchantStatus.None => 2,
                MerchantStatus.Mapped => 3,
                _ => 0,
            };
}
