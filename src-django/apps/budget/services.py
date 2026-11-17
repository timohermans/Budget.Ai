"""Budget summary calculation, ported from SummaryCalculator.cs."""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import date
from decimal import Decimal
from typing import TYPE_CHECKING

from apps.transactions.classification import is_fixed, is_variable

if TYPE_CHECKING:
    from collections.abc import Sequence

    from apps.transactions.models import Transaction


class DomainError(Exception):
    """Raised when a domain rule is violated."""


@dataclass(slots=True)
class TransactionTemplate:
    id: int
    amount: Decimal
    date: date
    is_fixed: bool
    is_not_fixed: bool
    name_other_party: str
    description: str | None
    logo_url: str | None = None
    display_name: str | None = None

    @property
    def display_name_or_raw(self) -> str:
        return self.display_name or self.name_other_party

    @property
    def css_class(self) -> str:
        return "positive" if self.amount > 0 else "negative"

    @property
    def show_toggle(self) -> bool:
        return self.is_fixed or self.is_not_fixed


@dataclass(slots=True)
class TransactionOverview:
    transaction: Transaction
    display_name: str | None = None
    logo_url: str | None = None

    @property
    def display_name_or_raw(self) -> str:
        return self.display_name or self.transaction.name_other_party


@dataclass(slots=True)
class WeekSummary:
    week_number: int
    budget: Decimal = Decimal("0")
    spent: Decimal = Decimal("0")
    left: Decimal = Decimal("0")
    transactions: list[TransactionTemplate] = field(default_factory=list)


@dataclass(slots=True)
class BalanceSummary:
    iban: str
    balance: Decimal = Decimal("0")
    transactions: list[TransactionTemplate] = field(default_factory=list)


@dataclass(slots=True)
class Summary:
    income: Decimal = Decimal("0")
    expenses: Decimal = Decimal("0")
    spent: Decimal = Decimal("0")
    left: Decimal = Decimal("0")
    budget: Decimal = Decimal("0")
    weeks: dict[int, WeekSummary] = field(default_factory=dict)
    income_transactions: list[TransactionTemplate] = field(default_factory=list)
    expense_transactions: list[TransactionTemplate] = field(default_factory=list)
    iban_balances: dict[str, BalanceSummary] = field(default_factory=dict)


def calculate(
    year: int,
    month: int,
    iban: str | None,
    transactions: Sequence[TransactionOverview],
    own_ibans: Sequence[str],
) -> Summary:
    """Compute the monthly budget summary for the given year and month."""
    this_month = date(year, month, 1)
    if month == 12:
        next_month = date(year + 1, 1, 1)
    else:
        next_month = date(year, month + 1, 1)
    if month == 1:
        last_month = date(year - 1, 12, 1)
    else:
        last_month = date(year, month - 1, 1)

    days_in_month = (next_month - this_month).days

    window = [item for item in transactions if last_month <= item.transaction.date < next_month]
    window.sort(
        key=lambda item: (item.transaction.date, item.display_name_or_raw),
        reverse=True,
    )

    if not own_ibans:
        return Summary()

    target_iban = iban or own_ibans[0]
    if target_iban not in own_ibans:
        raise DomainError("Iban does not exist.")

    dates_per_week: dict[int, int] = {}
    weeks: dict[int, WeekSummary] = {}
    for day in range(1, days_in_month + 1):
        date_in_month = date(year, month, day)
        week_number = date_in_month.isocalendar().week
        dates_per_week[week_number] = dates_per_week.get(week_number, 0) + 1
        weeks.setdefault(week_number, WeekSummary(week_number=week_number))

    summary = Summary(weeks=weeks)
    ibans = list(own_ibans)

    for result in window:
        transaction = result.transaction
        template = TransactionTemplate(
            id=transaction.id,
            amount=transaction.amount,
            date=transaction.date,
            is_fixed=is_fixed(transaction, ibans),
            is_not_fixed=transaction.is_not_fixed,
            name_other_party=transaction.name_other_party,
            description=transaction.description,
            logo_url=result.logo_url,
            display_name=result.display_name,
        )

        is_last_month = (
            transaction.date.year == last_month.year and transaction.date.month == last_month.month
        )
        is_this_month = not is_last_month
        week_number = transaction.date.isocalendar().week
        is_target_iban = target_iban == transaction.iban

        if is_target_iban and is_last_month and template.is_fixed and transaction.is_income():
            summary.income += transaction.amount
            summary.income_transactions.append(template)

        if is_target_iban and is_last_month and template.is_fixed and transaction.is_expense():
            summary.expenses += abs(transaction.amount)
            summary.expense_transactions.append(template)

        if is_target_iban and is_this_month:
            week_summary = summary.weeks[week_number]
            if is_variable(transaction, ibans):
                summary.spent += transaction.amount * -1
                week_summary.spent += transaction.amount * -1
            week_summary.transactions.append(template)

        if is_this_month:
            balance = summary.iban_balances.get(transaction.iban)
            if balance is None:
                balance = BalanceSummary(iban=transaction.iban)
                summary.iban_balances[transaction.iban] = balance
            balance.balance += transaction.amount
            balance.transactions.append(template)

    summary.budget = abs(summary.income) - abs(summary.expenses)
    summary.left = summary.budget - summary.spent

    for week in summary.weeks.values():
        week.budget = (
            summary.budget / Decimal(days_in_month) * Decimal(dates_per_week[week.week_number])
        )
        week.left = week.budget - week.spent

    return summary
