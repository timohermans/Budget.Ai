"""Transaction classification rules, ported from TransactionClassifier.cs."""

from __future__ import annotations

from collections.abc import Collection

from apps.transactions.models import Transaction

FIXED_CODES = frozenset({"sb", "cb", "bg", "ei", "tb"})


def is_fixed(transaction: Transaction, my_ibans: Collection[str]) -> bool:
    """Classify a transaction as fixed using the same precedence as .NET."""
    if transaction.is_not_fixed:
        return False
    if transaction.is_income() and transaction.is_from_own_account(my_ibans):
        return False
    if "paypal" in transaction.name_other_party.lower():
        return False
    if (
        transaction.code == "db"
        and transaction.description is not None
        and "sparen" in transaction.description.lower()
    ):
        return True
    if transaction.code == "db" and transaction.name_other_party == "Rabobank":
        return True
    if transaction.code in FIXED_CODES:
        return True
    return False


def is_variable(transaction: Transaction, my_ibans: Collection[str]) -> bool:
    return not is_fixed(transaction, my_ibans)
