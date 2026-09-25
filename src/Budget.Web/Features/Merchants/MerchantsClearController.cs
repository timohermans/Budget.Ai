using Budget.Web.Data;
using Budget.Web.Domain.Merchants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Features.Merchants;

[Route("merchants")]
public class MerchantsClearController(BudgetDbContext db, ILogger<MerchantsClearController> logger) : Controller
{

    [HttpPost("clear")]
    public async Task<IActionResult> Clear(
        string name, string search, string sort, string dir, CancellationToken ct)
    {
        var key = MerchantNameNormalizer.Normalize(name);

        if (key.Length > 0)
        {
            var merchant = await db.Merchants.SingleOrDefaultAsync(m => m.NameNormalized == key, ct);
            if (merchant is not null)
            {
                var aliases = await db.MerchantAliases
                    .Where(a => a.MerchantId == merchant.Id)
                    .ToListAsync(ct);
                db.MerchantAliases.RemoveRange(aliases);
                db.Merchants.Remove(merchant);
                logger.LogInformation("Cleared merchant {Name} and its {AliasCount} alias(es)", key, aliases.Count);
            }
            else
            {
                var alias = await db.MerchantAliases.SingleOrDefaultAsync(a => a.NameNormalized == key, ct);
                if (alias is not null)
                {
                    db.MerchantAliases.Remove(alias);
                    logger.LogInformation("Cleared alias {Name}", key);
                }
            }

            await db.SaveChangesAsync(ct);
        }

        var model = await MerchantListQuery.BuildRowsPartialAsync(db, search ?? "", sort ?? "", dir ?? "", ct);
        return PartialView("~/Views/Merchants/_Rows.cshtml", model);
    }
}
