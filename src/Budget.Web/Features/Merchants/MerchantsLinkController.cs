using Budget.Web.Data;
using Budget.Web.Domain.Merchants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Features.Merchants;

[Route("merchants")]
public class MerchantsLinkController(BudgetDbContext db) : Controller
{
    [HttpPost("link")]
    public async Task<IActionResult> Link(
        string name, string merchantName, string search, string sort, string dir, CancellationToken ct)
    {
        var key = MerchantNameNormalizer.Normalize(name);
        var targetKey = MerchantNameNormalizer.Normalize(merchantName);

        if (key.Length > 0 && targetKey.Length > 0 && key != targetKey)
        {
            var target = await db.Merchants.SingleOrDefaultAsync(m => m.NameNormalized == targetKey, ct);
            var alreadyCanonical = await db.Merchants.AnyAsync(m => m.NameNormalized == key, ct);
            if (target is not null && !alreadyCanonical)
            {
                var alias = await db.MerchantAliases.SingleOrDefaultAsync(a => a.NameNormalized == key, ct);
                if (alias is null)
                {
                    db.MerchantAliases.Add(new MerchantAlias
                    {
                        NameNormalized = key,
                        MerchantId = target.Id,
                        CreatedAt = DateTimeOffset.UtcNow,
                    });
                }
                else
                {
                    alias.MerchantId = target.Id;
                }

                await db.SaveChangesAsync(ct);
            }
        }

        var model = await MerchantListQuery.BuildRowsPartialAsync(db, search ?? "", sort ?? "", dir ?? "", ct);
        return PartialView("~/Views/Merchants/_Rows.cshtml", model);
    }
}
