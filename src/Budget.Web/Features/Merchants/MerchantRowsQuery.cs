using Budget.Web.Data;
using Budget.Web.Domain.Merchants;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Features.Merchants;

/// <summary>Shared query for the merchant admin rows list, used by the page and the htmx row swaps.</summary>
public static class MerchantRowsQuery
{
    public static async Task<List<MerchantRowModel>> GetRowsAsync(
        BudgetDbContext db, string search, string sort, string dir, CancellationToken ct)
    {
        var transactionsQuery = 
        from t in db.Transactions
        where !string.IsNullOrWhiteSpace(t.NameOtherPartyNormalized)
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

    /// <summary>Builds the rows partial for the full page render and for the htmx swaps after each action.</summary>
    public static async Task<RowsPartialModel> BuildRowsPartialAsync(
        BudgetDbContext db, string search, string sort, string dir, CancellationToken ct)
    {
        var rows = await GetRowsAsync(db, search, sort, dir, ct);
        var recentOptions = await MerchantOptionsQuery.GetOptionsAsync(db, null, ct);
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
