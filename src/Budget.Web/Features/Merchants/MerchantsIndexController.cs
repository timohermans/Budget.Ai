using Budget.Web.Data;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Web.Features.Merchants;

public class MerchantsViewModel
{
    public required List<MerchantRowModel> Rows { get; init; }
    public required List<MerchantOptionModel> RecentOptions { get; init; }
}

[Route("merchants")]
public class MerchantsIndexController(BudgetDbContext db) : Controller
{
    [Route("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var rows = await MerchantListQuery.GetRowsAsync(db, "", "", "", ct);
        var recentOptions = await MerchantListQuery.GetOptionsAsync(db, null, ct);
        return View("~/Views/Merchants/Index.cshtml", new MerchantsViewModel { Rows = rows, RecentOptions = recentOptions });
    }
}
