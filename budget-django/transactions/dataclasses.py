from dataclasses import field, dataclass
import datetime
from decimal import Decimal


@dataclass
class TransactionTemplateModel:
    id: int
    amount: Decimal
    date: datetime.date
    is_fixed: bool
    is_not_fixed: bool
    name_other_party: str
    description: str | None


@dataclass
class WeekSummary:
    week_number: int
    budget: Decimal
    spent: Decimal
    left: Decimal
    transactions: list[TransactionTemplateModel] = field(default_factory=list)


@dataclass
class BalanceSummary:
    iban: str
    balance: Decimal
    transactions: list[TransactionTemplateModel] = field(default_factory=list)


@dataclass
class Summary:
    income: Decimal
    expenses: Decimal
    spent: Decimal
    left: Decimal
    budget: Decimal
    weeks: dict[int, WeekSummary] = field(default_factory=dict)
    income_transactions: list[TransactionTemplateModel] = field(default_factory=list)
    expense_transactions: list[TransactionTemplateModel] = field(default_factory=list)
    iban_balances: dict[str, BalanceSummary] = field(default_factory=dict)
