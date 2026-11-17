"""Shared test helpers."""

from __future__ import annotations

from datetime import date
from typing import Any

from django.test import TestCase

from apps.transactions.models import Transaction


class BudgetDbTestCase(TestCase):
    """Base class for tests that use the managed business tables."""


def make_transaction(**overrides: Any) -> Transaction:
    values: dict[str, Any] = {
        "iban": "NL01RABO0000000001",
        "iban_other_party": "NL99RABO0000000000",
        "follow_number": 1,
        "user_id": "user-1",
        "amount": -10,
        "date": date(2026, 1, 3),
        "name_other_party": "Shop",
        "name_other_party_normalized": "shop",
        "code": "bc",
        "currency": "EUR",
        "description": None,
        "is_not_fixed": False,
    }
    values.update(overrides)
    return Transaction(**values)