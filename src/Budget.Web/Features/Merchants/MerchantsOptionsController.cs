using Budget.Web.Data;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Web.Features.Merchants;

public class OptionsPartialModel
{
    public required string Name { get; init; }
    public required string Q { get; init; }
    public required string Search { get; init; }
    public required string Sort { get; init; }
    public required string Dir { get; init; }
    public required List<MerchantOptionModel> Options { get; init; }
}

/// <summary>A candidate merchant in the link picker.</summary>
public class MerchantOptionModel
{
    public required string NameNormalized { get; init; }
    public required string Label { get; init; }
    public int TotalTransactions { get; init; }
    public DateTimeOffset? LastLinked {get; init;}
}

[Route("merchants")]
public class MerchantsOptionsController(BudgetDbContext db) : Controller
{
    /// <summary>Returns the link-picker candidate list, filtered and ordered, for the given row's name.</summary>
    [Route("options")]
    public async Task<IActionResult> Options(
        string name, string q, string search, string sort, string dir, CancellationToken ct)
    {
        var options = await MerchantListQuery.GetOptionsAsync(db, q, ct);
        return PartialView("~/Views/Merchants/_Options.cshtml", new OptionsPartialModel
        {
            Name = name ?? "",
            Q = q ?? "",
            Search = search ?? "",
            Sort = sort ?? "",
            Dir = dir ?? "",
            Options = options,
        });
    }
}
