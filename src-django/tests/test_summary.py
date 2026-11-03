from __future__ import annotations

from collections import Counter
from datetime import date
from decimal import Decimal

from django.test import SimpleTestCase

from apps.budget.services import DomainError, Summary, TransactionOverview, calculate
from apps.merchants.normalizer import normalize
from apps.transactions.models import Transaction

YEAR = 2099
MONTH = 1
FIRST_DAY = date(YEAR, MONTH, 1)
LAST_MONTH = date(2098, 12, 1)
MAIN_IBAN = "NL12RABO0123456789"
SAVINGS_IBAN = "NL12RABO0987654321"
OTHER_IBAN = "NL98INGB9876543210"


def transaction(
    follow: int,
    amount: str,
    day: date,
    code: str,
    name: str,
    iban: str,
    other: str,
    description: str | None = None,
) -> Transaction:
    return Transaction(
        id=follow,
        follow_number=follow,
        amount=Decimal(amount),
        date=day,
        code=code,
        name_other_party=name,
        name_other_party_normalized=normalize(name),
        iban=iban,
        iban_other_party=other,
        description=description,
        user_id="user-1",
        currency="EUR",
    )


def calculate_for(transactions: list[Transaction], selected_iban: str | None = None) -> Summary:
    counts = Counter(item.iban for item in transactions)
    own_ibans = [iban for iban, _ in counts.most_common()]
    return calculate(
        YEAR,
        MONTH,
        selected_iban,
        [TransactionOverview(item, None, None) for item in transactions],
        own_ibans,
    )


class SummaryCalculatorTests(SimpleTestCase):
    def test_calculate_baseline(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1, "3000", LAST_MONTH.replace(day=15), "sb", "Employer", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    2,
                    "-800",
                    LAST_MONTH.replace(day=15),
                    "cb",
                    "Insurance Co",
                    MAIN_IBAN,
                    OTHER_IBAN,
                ),
                transaction(
                    3, "-150", FIRST_DAY.replace(day=3), "bc", "Albert Heijn", MAIN_IBAN, OTHER_IBAN
                ),
            ]
        )
        self.assertEqual(3000, summary.income)
        self.assertEqual(800, summary.expenses)
        self.assertEqual(2200, summary.budget)
        self.assertEqual(150, summary.spent)
        self.assertEqual(2050, summary.left)

    def test_calculate_spent_only_variable(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1, "3000", LAST_MONTH.replace(day=15), "sb", "Employer", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    2,
                    "-800",
                    LAST_MONTH.replace(day=15),
                    "cb",
                    "Insurance Co",
                    MAIN_IBAN,
                    OTHER_IBAN,
                ),
                transaction(
                    3, "-150", FIRST_DAY.replace(day=3), "bc", "Albert Heijn", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    4, "-50", FIRST_DAY.replace(day=5), "bc", "Hema", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    5, "-100", FIRST_DAY.replace(day=10), "ie", "Bol.com", MAIN_IBAN, OTHER_IBAN
                ),
            ]
        )
        self.assertEqual(300, summary.spent)

    def test_calculate_no_ibans_returns_empty(self) -> None:
        summary = calculate(YEAR, MONTH, None, [], [])
        self.assertEqual(0, summary.income)
        self.assertEqual(0, summary.expenses)
        self.assertEqual(0, summary.spent)
        self.assertEqual(0, summary.left)
        self.assertEqual(0, summary.budget)
        self.assertEqual({}, summary.weeks)
        self.assertEqual({}, summary.iban_balances)

    def test_calculate_week_budget_proportional_to_days(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1, "3000", LAST_MONTH.replace(day=15), "sb", "Employer", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    2,
                    "-800",
                    LAST_MONTH.replace(day=15),
                    "cb",
                    "Insurance Co",
                    MAIN_IBAN,
                    OTHER_IBAN,
                ),
            ]
        )
        expected = {
            1: Decimal("283.87"),
            2: Decimal("496.77"),
            3: Decimal("496.77"),
            4: Decimal("496.77"),
            5: Decimal("425.81"),
        }
        for week_number, amount in expected.items():
            with self.subTest(week=week_number):
                self.assertEqual(
                    amount, summary.weeks[week_number].budget.quantize(Decimal("0.01"))
                )

    def test_calculate_positive_variable_reduces_spent(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1, "3000", LAST_MONTH.replace(day=15), "sb", "Employer", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    2,
                    "-800",
                    LAST_MONTH.replace(day=15),
                    "cb",
                    "Insurance Co",
                    MAIN_IBAN,
                    OTHER_IBAN,
                ),
                transaction(
                    3, "50", FIRST_DAY.replace(day=3), "bc", "Refund Shop", MAIN_IBAN, OTHER_IBAN
                ),
            ]
        )
        self.assertEqual(-50, summary.weeks[1].spent)
        self.assertEqual(
            Decimal("333.87"),
            summary.weeks[1].left.quantize(Decimal("0.01")),
        )

    def test_calculate_fixed_income_from_last_month(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1, "2000", LAST_MONTH.replace(day=1), "sb", "Employer", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    2,
                    "1000",
                    LAST_MONTH.replace(day=10),
                    "sb",
                    "Freelance Client",
                    MAIN_IBAN,
                    OTHER_IBAN,
                ),
                transaction(
                    3, "500", LAST_MONTH.replace(day=20), "sb", "Side Gig", MAIN_IBAN, OTHER_IBAN
                ),
            ]
        )
        self.assertEqual(3500, summary.income)

    def test_calculate_fixed_expenses_from_last_month(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1, "3000", LAST_MONTH.replace(day=10), "sb", "Employer", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    2, "-800", LAST_MONTH.replace(day=12), "cb", "Insurance", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    3, "-400", LAST_MONTH.replace(day=18), "cb", "Mortgage", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    4, "-300", LAST_MONTH.replace(day=25), "cb", "Netflix", MAIN_IBAN, OTHER_IBAN
                ),
            ]
        )
        self.assertEqual(1500, summary.expenses)

    def test_calculate_own_account_transfer_excluded(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1, "3000", LAST_MONTH.replace(day=15), "sb", "Employer", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    2, "500", LAST_MONTH.replace(day=16), "db", "Rabobank", MAIN_IBAN, SAVINGS_IBAN
                ),
                transaction(
                    3,
                    "-1000",
                    LAST_MONTH.replace(day=17),
                    "cb",
                    "Insurance Co",
                    SAVINGS_IBAN,
                    OTHER_IBAN,
                ),
            ]
        )
        self.assertEqual(3000, summary.income)
        self.assertEqual(3000, summary.budget)

    def test_calculate_paypal_excluded(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1, "3000", LAST_MONTH.replace(day=15), "sb", "Employer", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    2, "500", LAST_MONTH.replace(day=15), "sb", "PayPal EU", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    3, "-300", LAST_MONTH.replace(day=15), "cb", "PayPal EU", MAIN_IBAN, OTHER_IBAN
                ),
            ]
        )
        self.assertEqual(3000, summary.income)
        self.assertEqual(0, summary.expenses)

    def test_calculate_multiple_iban_balances(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1, "3000", LAST_MONTH.replace(day=15), "sb", "Employer", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    2,
                    "-800",
                    LAST_MONTH.replace(day=15),
                    "cb",
                    "Insurance Co",
                    MAIN_IBAN,
                    OTHER_IBAN,
                ),
                transaction(
                    3, "-150", FIRST_DAY.replace(day=3), "bc", "Albert Heijn", MAIN_IBAN, OTHER_IBAN
                ),
                transaction(
                    4,
                    "2000",
                    FIRST_DAY.replace(day=12),
                    "sb",
                    "Freelance Client",
                    OTHER_IBAN,
                    MAIN_IBAN,
                ),
            ]
        )
        self.assertEqual(2, len(summary.iban_balances))
        self.assertEqual(-150, summary.iban_balances[MAIN_IBAN].balance)
        self.assertEqual(2000, summary.iban_balances[OTHER_IBAN].balance)

    def test_calculate_explicit_iban(self) -> None:
        summary = calculate_for(
            [
                transaction(
                    1,
                    "3000",
                    LAST_MONTH.replace(day=15),
                    "sb",
                    "Employer",
                    MAIN_IBAN,
                    "NL99EXTERNAL1",
                ),
                transaction(
                    2,
                    "1000",
                    LAST_MONTH.replace(day=15),
                    "sb",
                    "Client",
                    OTHER_IBAN,
                    "NL99EXTERNAL2",
                ),
                transaction(
                    3, "-200", FIRST_DAY.replace(day=3), "bc", "Shop", OTHER_IBAN, "NL99EXTERNAL3"
                ),
            ],
            selected_iban=OTHER_IBAN,
        )
        self.assertEqual(1000, summary.income)
        self.assertEqual(200, summary.spent)

    def test_calculate_unknown_iban_raises(self) -> None:
        with self.assertRaises(DomainError):
            calculate_for(
                [
                    transaction(
                        1,
                        "-150",
                        FIRST_DAY.replace(day=3),
                        "bc",
                        "Albert Heijn",
                        MAIN_IBAN,
                        OTHER_IBAN,
                    )
                ],
                selected_iban="NL99UNKNOWN0000000",
            )
