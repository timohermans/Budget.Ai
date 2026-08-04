using Budget.Web.Data;
using Budget.Web.Domain.Merchants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Features.Merchants;

[Route("merchants")]
public class MerchantsMapController(BudgetDbContext db) : Controller
{
    [HttpPost("map")]
    public async Task<IActionResult> Map(
        string name, string? displayName, string? logoUrl, string search, string sort, string dir, CancellationToken ct)
    {
        var key = MerchantNameNormalizer.Normalize(name);
        var validLogoUrl = !string.IsNullOrWhiteSpace(logoUrl)
            && Uri.TryCreate(logoUrl.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        if (key.Length > 0 && validLogoUrl)
        {
            displayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();

            var merchant = await db.Merchants.SingleOrDefaultAsync(m => m.NameNormalized == key, ct);
            if (merchant is null)
            {
                db.Merchants.Add(new Merchant
                {
                    NameNormalized = key,
                    DisplayName = displayName,
                    LogoUrl = logoUrl?.Trim(),
                    Status = MerchantStatus.Mapped,
                    UpdatedAt = DateTimeOffset.UtcNow,
                });
            }
            else
            {
                merchant.DisplayName = displayName;
                merchant.LogoUrl = logoUrl?.Trim();
                merchant.Status = MerchantStatus.Mapped;
                merchant.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(ct);
        }

        var model = await MerchantListQuery.BuildRowsPartialAsync(db, search ?? "", sort ?? "", dir ?? "", ct);
        return PartialView("~/Views/Merchants/_Rows.cshtml", model);
    }
}
