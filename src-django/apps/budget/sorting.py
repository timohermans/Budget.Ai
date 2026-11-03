"""Overview transaction sorting, ported from OverviewSort.cs."""

from __future__ import annotations

from collections.abc import Callable
from functools import cmp_to_key

from apps.budget.services import TransactionTemplate

DATE_DESC = "date-desc"
DATE_ASC = "date-asc"
AMOUNT_DESC = "amount-desc"
AMOUNT_ASC = "amount-asc"
NAME_ASC = "name-asc"
NAME_DESC = "name-desc"

QUERY_TO_SORT = {
    "datum-begin-eind": DATE_ASC,
    "hoogste-uitgaves": AMOUNT_DESC,
    "kleinste-uitgaves": AMOUNT_ASC,
    "winkel-a-z": NAME_ASC,
    "winkel-z-a": NAME_DESC,
}

SORT_TO_QUERY = {value: key for key, value in QUERY_TO_SORT.items()}


def parse_sort(value: str | None) -> str:
    return QUERY_TO_SORT.get(value or "", DATE_DESC)


def to_query(sort: str) -> str | None:
    return SORT_TO_QUERY.get(sort)


def _compare_date_desc(a: TransactionTemplate, b: TransactionTemplate) -> int:
    if a.date == b.date:
        return 0
    return -1 if a.date > b.date else 1


def _compare_name_desc(a: TransactionTemplate, b: TransactionTemplate) -> int:
    name_a = a.display_name_or_raw
    name_b = b.display_name_or_raw
    if name_a == name_b:
        return 0
    return -1 if name_a > name_b else 1


def _compare_name_ignore_case(a: TransactionTemplate, b: TransactionTemplate) -> int:
    name_a = a.display_name_or_raw.lower()
    name_b = b.display_name_or_raw.lower()
    if name_a == name_b:
        return 0
    return -1 if name_a < name_b else 1


def _expense_pivot(item: TransactionTemplate) -> int:
    return 0 if item.amount < 0 else 1


def _comparator(sort: str) -> Callable[[TransactionTemplate, TransactionTemplate], int]:
    def compare(a: TransactionTemplate, b: TransactionTemplate) -> int:
        if sort == DATE_ASC:
            if a.date != b.date:
                return -1 if a.date < b.date else 1
            return _compare_name_desc(a, b)
        if sort == AMOUNT_DESC:
            if a.amount != b.amount:
                return -1 if a.amount < b.amount else 1
            return _compare_date_desc(a, b)
        if sort == AMOUNT_ASC:
            pivot = _expense_pivot(a) - _expense_pivot(b)
            if pivot != 0:
                return pivot
            size = abs(a.amount) - abs(b.amount)
            if size != 0:
                return -1 if size < 0 else 1
            return _compare_date_desc(a, b)
        if sort == NAME_ASC:
            name = _compare_name_ignore_case(a, b)
            return name if name != 0 else _compare_date_desc(a, b)
        if sort == NAME_DESC:
            name = _compare_name_ignore_case(b, a)
            return name if name != 0 else _compare_date_desc(a, b)
        date = _compare_date_desc(a, b)
        return date if date != 0 else _compare_name_desc(a, b)

    return compare


def sort_transactions(items: list[TransactionTemplate], sort: str) -> None:
    items.sort(key=cmp_to_key(_comparator(sort)))
