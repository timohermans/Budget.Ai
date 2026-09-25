using Budget.Web.Domain.Transactions;
using Budget.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Budget.Web.Features.Transactions;

/// <summary>Handles Rabobank CSV uploads.</summary>
[Route("transactions")]
public class UploadController(RabobankCsvImporter importer, ILogger<UploadController> logger) : Controller
{

    /// <summary>Imports the uploaded Rabobank CSV and redirects to the month of the most recent transaction, or renders the error view on failure.</summary>
    /// <param name="file">The uploaded CSV file.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    [HttpPost("upload")]
    public async Task<IActionResult> Index(IFormFile? file, CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (file is null)
        {
            logger.LogWarning("CSV upload for user {UserId} rejected: no file provided", userId);
            return View("Error");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var maxDate = await importer.ProcessAsync(stream, userId, ct);
            logger.LogInformation(
                "Imported Rabobank CSV {FileName} ({FileSize} bytes) for user {UserId}; redirecting to {Year}-{Month:00}",
                file.FileName, file.Length, userId, maxDate.Year, maxDate.Month);
            return Redirect($"/budget/{maxDate.Year}/{maxDate.Month}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rabobank CSV import failed for user {UserId} (file: {FileName})", userId, file.FileName);
            return View("Error");
        }
    }
}
