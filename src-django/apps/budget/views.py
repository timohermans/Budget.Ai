"""Budget overview view, ported from OverviewController.cs."""

from __future__ import annotations

from datetime import date, timedelta

from django.http import HttpRequest, HttpResponse, HttpResponseBadRequest
from django.shortcuts import render
from django.utils import timezone

from apps.budget import queries, services
from apps.budget.services import DomainError
from apps.budget.sorting import DATE_DESC, parse_sort, sort_transactions, to_query
from config.auth import get_user_id

MONTH_NAMES = (
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December",
)
MONTH_ABBREVIATIONS = (
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
)


def _shift_month(value: date, months: int) -> date:
    month_index = value.year * 12 + (value.month - 1) + months
    return date(month_index // 12, month_index % 12 + 1, 1)


def overview(
    request: HttpRequest,
    year: int | None = None,
    month: int | None = None,
    week_or_iban: str | None = None,
) -> HttpResponse:
    """Render the monthly budget overview or one of its htmx fragments."""
    today = timezone.localdate()
    year = year if year and year > 0 else today.year
    month = month if month and month > 0 else today.month
    if not 1 <= month <= 12:
        return HttpResponseBadRequest("Invalid month.")

    week = 0
    iban: str | None = None
    if week_or_iban is not None:
        try:
            week = int(week_or_iban)
        except ValueError:
            iban = week_or_iban

    user_id = get_user_id(request)
    first_of_month = date(year, month, 1)
    last_month = _shift_month(first_of_month, -1)
    next_month = _shift_month(first_of_month, 1)
    last_of_month = next_month - timedelta(days=1)

    own_ibans = queries.get_own_ibans(user_id)
    transactions = queries.get_transaction_overview(user_id, last_month, next_month)

    try:
        summary = services.calculate(year, month, iban, transactions, own_ibans)
    except DomainError as error:
        return HttpResponseBadRequest(str(error))

    sort_order = parse_sort(request.GET.get("sort"))
    if sort_order != DATE_DESC:
        for week_summary in summary.weeks.values():
            sort_transactions(week_summary.transactions, sort_order)
        for balance in summary.iban_balances.values():
            sort_transactions(balance.transactions, sort_order)

    sort_query = to_query(sort_order)
    sort_suffix = f"?sort={sort_query}" if sort_query else ""
    current_path = f"/budget/{year}/{month}"
    if week > 0:
        current_path = f"{current_path}/{week}"
    elif iban is not None:
        current_path = f"{current_path}/{iban}"

    fixed = request.GET.get("fixed")
    fixed_is_income = bool(fixed and fixed.lower() == "income")
    fixed_transactions = (
        summary.income_transactions if fixed_is_income else summary.expense_transactions
    )
    fixed_transactions = sorted(fixed_transactions, key=lambda item: item.amount)

    context = {
        "year": year,
        "month": month,
        "week": week,
        "iban": iban,
        "summary": summary,
        "fixed": fixed,
        "fixed_is_income": fixed_is_income,
        "fixed_transactions": fixed_transactions,
        "fixed_label": "Vaste inkomsten" if fixed_is_income else "Vaste uitgaven",
        "sort": sort_order,
        "current_path": current_path,
        "sort_suffix": sort_suffix,
        "previous_url": f"/budget/{last_month.year}/{last_month.month}{sort_suffix}",
        "today_url": f"/budget/{sort_suffix}",
        "next_url": f"/budget/{next_month.year}/{next_month.month}{sort_suffix}",
        "month_display": MONTH_NAMES[month - 1],
        "month_start": f"{first_of_month.day:02d} {MONTH_ABBREVIATIONS[month - 1]}",
        "month_end": f"{last_of_month.day:02d} {MONTH_ABBREVIATIONS[month - 1]}",
    }

    if fixed is not None and request.headers.get("HX-Request"):
        return render(request, "budget/_fixed_transactions.html", context)
    return render(request, "budget/overview.html", context)
