using Budget.Web.Data;
using Budget.Web.Domain.Merchants;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Web.Features.Merchants;

public class RowsPartialModel
{
    public required string Search { get; init; }
    public required string Sort { get; init; }
    public required string Dir { get; init; }
    public required List<MerchantRowModel> Rows { get; init; }
    public required List<MerchantOptionModel> RecentOptions { get; init; }
}

/// <summary>A row on the merchant admin page, representing one distinct normalized counterparty name.</summary>
public class MerchantRowModel
{
    public required string NameNormalized { get; init; }
    public required string RawName { get; init; }
    public int TransactionCount { get; init; }
    public DateOnly FirstSeen { get; init; }
    public MerchantStatus? Status { get; init; }
    public string? DisplayName { get; init; }
    public string? LogoUrl { get; init; }
    public string? LinkedToName { get; init; }
    public bool IsLinked => LinkedToName is not null;
}

[Route("merchants")]
public class MerchantsRowsController(BudgetDbContext db) : Controller
{
    /// <summary>Returns just the rows partial for the search/sort htmx requests and after each action.</summary>
    [Route("rows")]
    public async Task<IActionResult> Rows(string search, string sort, string dir, CancellationToken ct)
    {
        var model = await MerchantListQuery.BuildRowsPartialAsync(db, search ?? "", sort ?? "", dir ?? "", ct);
        return PartialView("~/Views/Merchants/_Rows.cshtml", model);
    }
}
