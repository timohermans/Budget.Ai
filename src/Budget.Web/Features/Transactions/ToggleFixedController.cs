using System.Globalization;
using Budget.Web.Data;
using Budget.Web.Domain.Transactions;
using Budget.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Features.Transactions;

/// <summary>Handles toggling the fixed status of a transaction.</summary>
[Route("transactions")]
public class ToggleFixedController(BudgetDbContext db) : Controller
{
    /// <summary>
    /// Toggles the transaction's fixed status and returns the updated toggle fragment with out-of-band
    /// summary swaps for the affected month and week.
    /// </summary>
    /// <param name="id">The id of the transaction to toggle.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    [HttpPost("toggle-fixed")]
    public async Task<IActionResult> Index(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var transaction = await db.Transactions
            .SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (transaction is null)
            return NotFound();

        transaction.IsNotFixed = !transaction.IsNotFixed;
        await db.SaveChangesAsync(ct);

        var ownIbans = await db.Transactions
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.Iban)
            .Select(g => new { Iban = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => x.Iban)
            .ToListAsync(ct);

        var date = transaction.Date;
        var firstOfMonth = new DateOnly(date.Year, date.Month, 1);
        var lastMonth = firstOfMonth.AddMonths(-1);
        var nextMonth = firstOfMonth.AddMonths(1);

        var window = await db.Transactions
            .Where(t => t.UserId == userId && t.Date >= lastMonth && t.Date < nextMonth)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.NameOtherParty)
            .Select(t => new TransactionOverviewQueryResult(t, null, null))
            .ToListAsync(ct);

        var summary = SummaryCalculator.Calculate(date.Year, date.Month, null, window, ownIbans);
        var week = ISOWeek.GetWeekOfYear(date);
        var weekSummary = summary.Weeks[week];

        var model = new ToggleFixedPartialModel(
            new TransactionTemplateModel(
                transaction.Id,
                transaction.Amount,
                transaction.Date,
                TransactionClassifier.IsFixed(transaction, ownIbans),
                transaction.IsNotFixed,
                transaction.NameOtherParty,
                transaction.Description),
            week,
            summary.Spent,
            summary.Left,
            weekSummary.Spent,
            weekSummary.Left,
            weekSummary.Budget);

        return PartialView("_ToggleFixed", model);
    }
}
