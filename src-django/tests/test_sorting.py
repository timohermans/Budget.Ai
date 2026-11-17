from __future__ import annotations

from datetime import date
from decimal import Decimal

from django.test import SimpleTestCase

from apps.budget.services import TransactionTemplate
from apps.budget.sorting import (
    AMOUNT_ASC,
    AMOUNT_DESC,
    DATE_ASC,
    DATE_DESC,
    NAME_ASC,
    NAME_DESC,
    parse_sort,
    sort_transactions,
    to_query,
)


def item(
    identifier: int,
    amount: str,
    day: int,
    name: str,
    display_name: str | None = None,
) -> TransactionTemplate:
    return TransactionTemplate(
        id=identifier,
        amount=Decimal(amount),
        date=date(2099, 1, day),
        is_fixed=False,
        is_not_fixed=False,
        name_other_party=name,
        description=None,
        display_name=display_name,
    )


class OverviewSortTests(SimpleTestCase):
    def test_parse_known_values(self) -> None:
        expected = {
            "datum-begin-eind": DATE_ASC,
            "hoogste-uitgaves": AMOUNT_DESC,
            "kleinste-uitgaves": AMOUNT_ASC,
            "winkel-a-z": NAME_ASC,
            "winkel-z-a": NAME_DESC,
        }
        for value, sort in expected.items():
            self.assertEqual(sort, parse_sort(value))

    def test_parse_unknown_values_default(self) -> None:
        for value in (None, "", "garbage", "DATUM-BEGIN-EIND", "datum-begin-eind "):
            self.assertEqual(DATE_DESC, parse_sort(value))

    def test_to_query_default_is_none_and_round_trips(self) -> None:
        self.assertIsNone(to_query(DATE_DESC))
        for sort in (DATE_ASC, AMOUNT_DESC, AMOUNT_ASC, NAME_ASC, NAME_DESC):
            self.assertEqual(sort, parse_sort(to_query(sort)))

    def test_sort_default_date_desc_then_name_desc(self) -> None:
        items = [
            item(1, "1000", 5, "Salaris"),
            item(2, "-450", 6, "Huur"),
            item(3, "-50", 7, "Albert Heijn"),
            item(4, "-3.50", 8, "Bol.com"),
            item(5, "25", 9, "Terugbetaling"),
        ]
        sort_transactions(items, DATE_DESC)
        self.assertEqual(
            ["Terugbetaling", "Bol.com", "Albert Heijn", "Huur", "Salaris"],
            [entry.name_other_party for entry in items],
        )

    def test_sort_amount_desc(self) -> None:
        items = [
            item(1, "1000", 5, "Salaris"),
            item(2, "-450", 6, "Huur"),
            item(3, "-50", 7, "Albert Heijn"),
            item(4, "-3.50", 8, "Bol.com"),
            item(5, "25", 9, "Terugbetaling"),
        ]
        sort_transactions(items, AMOUNT_DESC)
        self.assertEqual(
            ["Huur", "Albert Heijn", "Bol.com", "Terugbetaling", "Salaris"],
            [entry.name_other_party for entry in items],
        )

    def test_sort_amount_asc_places_expenses_first_by_absolute_size(self) -> None:
        items = [
            item(1, "1000", 5, "Salaris"),
            item(2, "-450", 6, "Huur"),
            item(3, "-50", 7, "Albert Heijn"),
            item(4, "-3.50", 8, "Bol.com"),
            item(5, "25", 9, "Terugbetaling"),
        ]
        sort_transactions(items, AMOUNT_ASC)
        self.assertEqual(
            ["Bol.com", "Albert Heijn", "Huur", "Terugbetaling", "Salaris"],
            [entry.name_other_party for entry in items],
        )

    def test_sort_date_asc(self) -> None:
        items = [
            item(1, "1000", 5, "Salaris"),
            item(2, "-450", 6, "Huur"),
            item(3, "-50", 7, "Albert Heijn"),
            item(4, "-3.50", 8, "Bol.com"),
            item(5, "25", 9, "Terugbetaling"),
        ]
        sort_transactions(items, DATE_ASC)
        self.assertEqual(
            ["Salaris", "Huur", "Albert Heijn", "Bol.com", "Terugbetaling"],
            [entry.name_other_party for entry in items],
        )

    def test_sort_names_use_display_name_and_ignore_case(self) -> None:
        items = [
            item(1, "-10", 5, "salaris", display_name="Salaris"),
            item(2, "-10", 6, "Huur"),
            item(3, "-10", 7, "albert heijn", display_name="Albert Heijn"),
            item(4, "-10", 8, "Bol.com"),
            item(5, "-10", 9, "Terugbetaling"),
        ]
        sort_transactions(items, NAME_ASC)
        self.assertEqual(
            ["Albert Heijn", "Bol.com", "Huur", "Salaris", "Terugbetaling"],
            [entry.display_name_or_raw for entry in items],
        )
        sort_transactions(items, NAME_DESC)
        self.assertEqual(
            ["Terugbetaling", "Salaris", "Huur", "Bol.com", "Albert Heijn"],
            [entry.display_name_or_raw for entry in items],
        )

    def test_equal_amounts_tie_break_by_date_desc(self) -> None:
        items = [item(1, "-10", 5, "Albert Heijn"), item(2, "-10", 6, "Jumbo")]
        sort_transactions(items, AMOUNT_DESC)
        self.assertEqual(["Jumbo", "Albert Heijn"], [entry.name_other_party for entry in items])
