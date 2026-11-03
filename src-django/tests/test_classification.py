from __future__ import annotations

from django.test import SimpleTestCase

from apps.transactions.classification import is_fixed, is_variable
from tests.base import make_transaction

MY_IBANS = ["NL12RABO0123456789", "NL12RABO0987654321"]


class TransactionClassifierTests(SimpleTestCase):
    def test_is_fixed_when_flagged_variable_then_false(self) -> None:
        transaction = make_transaction(amount=-100, code="cb", is_not_fixed=True)
        self.assertFalse(is_fixed(transaction, MY_IBANS))

    def test_is_fixed_when_income_from_own_account_then_false(self) -> None:
        transaction = make_transaction(
            amount=500,
            code="sb",
            name_other_party="Rabobank",
            iban_other_party="NL12RABO0987654321",
        )
        self.assertFalse(is_fixed(transaction, MY_IBANS))

    def test_is_fixed_when_expense_from_own_account_then_true(self) -> None:
        transaction = make_transaction(
            amount=-500,
            code="tb",
            name_other_party="Spaar",
            iban_other_party="NL12RABO0987654321",
        )
        self.assertTrue(is_fixed(transaction, MY_IBANS))

    def test_is_fixed_when_counterparty_contains_paypal_then_false(self) -> None:
        transaction = make_transaction(amount=-100, code="cb", name_other_party="PayPal EU")
        self.assertFalse(is_fixed(transaction, MY_IBANS))

    def test_is_fixed_when_db_and_description_contains_sparen_then_true(self) -> None:
        transaction = make_transaction(
            amount=-1000,
            code="db",
            name_other_party="Spaar",
            description="Maandelijks sparen",
        )
        self.assertTrue(is_fixed(transaction, MY_IBANS))

    def test_is_fixed_when_db_and_rabobank_then_true(self) -> None:
        transaction = make_transaction(
            amount=-5.45,
            code="db",
            name_other_party="Rabobank",
            description="Kosten basispakket",
        )
        self.assertTrue(is_fixed(transaction, MY_IBANS))

    def test_is_fixed_when_known_code_then_true(self) -> None:
        for code in ("sb", "cb", "bg", "ei", "tb"):
            with self.subTest(code=code):
                self.assertTrue(is_fixed(make_transaction(code=code), MY_IBANS))

    def test_is_fixed_when_no_rule_matches_then_false(self) -> None:
        transaction = make_transaction(amount=-150, code="bc", name_other_party="Albert Heijn")
        self.assertFalse(is_fixed(transaction, MY_IBANS))

    def test_is_variable_returns_inverse(self) -> None:
        self.assertFalse(is_variable(make_transaction(code="cb"), MY_IBANS))
        self.assertTrue(is_variable(make_transaction(code="bc"), MY_IBANS))
