using System.Globalization;
using Budget.Web.Data;
using Budget.Web.Domain.Transactions;
using Budget.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Features.Budget;

/// <summary>Serves the monthly budget overview page.</summary>
[Route("budget")]
public class OverviewController(BudgetDbContext db) : Controller
{
    /// <summary>
    /// Renders the budget overview for the given year and month, optionally expanding a week or selecting
    /// an IBAN as the main account. Defaults to the current month when year and month are not given.
    /// </summary>
    /// <param name="year">The year of the month to show, or 0 for the current year.</param>
    /// <param name="month">The month to show, or 0 for the current month.</param>
    /// <param name="weekOrIban">A week number to expand or an IBAN to select as the main account.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    [Route("")]
    [Route("{year:int}/{month:int}")]
    [Route("{year:int}/{month:int}/{weekOrIban}")]
    public async Task<IActionResult> Index(int year, int month, string? weekOrIban, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (year == 0)
            year = today.Year;
        if (month == 0)
            month = today.Month;

        var week = 0;
        var isIban = weekOrIban is not null && !int.TryParse(weekOrIban, out week);
        var iban = isIban ? weekOrIban : null;

        var userId = User.GetUserId();

        var date = new DateOnly(year, month, 1);
        var lastMonth = date.AddMonths(-1);
        var nextMonth = date.AddMonths(1);

        var ownIbans = await db.Transactions
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.Iban)
            .Select(g => new { Iban = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => x.Iban)
            .ToListAsync(ct);

        var window = await db.Transactions
            .Where(t => t.UserId == userId && t.Date >= lastMonth && t.Date < nextMonth)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.NameOtherParty)
            .ToListAsync(ct);

        var summary = SummaryCalculator.Calculate(year, month, iban, window, ownIbans);

        var viewModel = new OverviewViewModel
        {
            Year = year,
            Month = month,
            Week = week,
            Iban = iban,
            Date = date,
            DatePrevious = lastMonth,
            DateNext = nextMonth,
            MonthDisplay = date.ToString("MMMM", CultureInfo.InvariantCulture),
            MonthStart = date.ToString("dd MMM", CultureInfo.InvariantCulture),
            MonthEnd = nextMonth.AddDays(-1).ToString("dd MMM", CultureInfo.InvariantCulture),
            Summary = summary,
        };

        return View(viewModel);
    }
}

public class OverviewViewModel
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required int Week { get; init; }
    public required string? Iban { get; init; }
    public required DateOnly Date { get; init; }
    public required DateOnly DatePrevious { get; init; }
    public required DateOnly DateNext { get; init; }
    public required string MonthDisplay { get; init; }
    public required string MonthStart { get; init; }
    public required string MonthEnd { get; init; }
    public required Summary Summary { get; init; }
}
