using Budget.Web.Domain.Transactions;
using Budget.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Web.Features.Transactions;

/// <summary>Handles Rabobank CSV uploads.</summary>
[Route("transactions")]
public class UploadController(RabobankCsvImporter importer) : Controller
{
    /// <summary>Imports the uploaded Rabobank CSV and redirects to the month of the most recent transaction, or renders the error view on failure.</summary>
    /// <param name="file">The uploaded CSV file.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    [HttpPost("upload")]
    [TestModeAwareValidateAntiforgeryToken]
    public async Task<IActionResult> Index(IFormFile? file, CancellationToken ct)
    {
        if (file is null)
            return View("Error");

        try
        {
            await using var stream = file.OpenReadStream();
            var maxDate = await importer.ProcessAsync(stream, User.GetUserId(), ct);
            return Redirect($"/budget/{maxDate.Year}/{maxDate.Month}");
        }
        catch
        {
            return View("Error");
        }
    }
}
