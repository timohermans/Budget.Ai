from __future__ import annotations

from datetime import date

from django.test import SimpleTestCase

from apps.transactions import importer
from apps.transactions.models import Transaction
from tests.base import BudgetDbTestCase

HEADER = (
    '"IBAN/BBAN","Munt","BIC","Volgnr","Datum","Rentedatum","Bedrag","Saldo na trn",'
    '"Tegenrekening IBAN/BBAN","Naam tegenpartij","Naam uiteindelijke partij",'
    '"Naam initiërende partij","BIC tegenpartij","Code","Batch ID","Transactiereferentie",'
    '"Machtigingskenmerk","Incassant ID","Betalingskenmerk","Omschrijving-1",'
    '"Omschrijving-2","Omschrijving-3","Reden retour","Oorspr bedrag","Oorspr munt","Koers"'
)
ROW = (
    '"OWNED1","EUR","RABONL2U","000000000000000001","2026-01-03","2026-01-03",'
    '"-150,00","","THEIRS1","Albert Heijn","","","","bc","","","","","",'
    '"Groceries","","","","","",""'
)


class ImporterParseTests(SimpleTestCase):
    def test_parse_when_only_header_then_empty(self) -> None:
        self.assertEqual([], importer.parse(HEADER.encode("latin-1"), "user-1"))

    def test_parse_when_single_row_then_parses_all_fields(self) -> None:
        result = importer.parse((HEADER + "\n" + ROW).encode("latin-1"), "user-1")
        transaction = result[0]
        self.assertEqual("OWNED1", transaction.iban)
        self.assertEqual("EUR", transaction.currency)
        self.assertEqual(date(2026, 1, 3), transaction.date)
        self.assertEqual(-150, transaction.amount)
        self.assertEqual("THEIRS1", transaction.iban_other_party)
        self.assertEqual("Albert Heijn", transaction.name_other_party)
        self.assertEqual("bc", transaction.code)
        self.assertEqual("Groceries", transaction.description)
        self.assertEqual(1, transaction.follow_number)
        self.assertEqual("user-1", transaction.user_id)

    def test_parse_when_name_given_then_stores_normalized_name(self) -> None:
        row = ROW.replace("Albert Heijn", "AH- Jan Linders 4181")
        result = importer.parse((HEADER + "\n" + row).encode("latin-1"), "user-1")
        self.assertEqual("ah - jan linders 4181", result[0].name_other_party_normalized)

    def test_parse_when_multiple_rows_then_parses_each(self) -> None:
        row2 = (
            ROW.replace("2026-01-03", "2026-01-05")
            .replace("-150,00", "3000,00")
            .replace("000000000000000001", "000000000000000002")
        )
        result = importer.parse((HEADER + "\n" + ROW + "\n" + row2).encode("latin-1"), "user-1")
        self.assertEqual(2, len(result))
        self.assertEqual(-150, result[0].amount)
        self.assertEqual(3000, result[1].amount)

    def test_parse_when_description_concatenated_then_trimmed(self) -> None:
        row = ROW.replace('"Groceries","",""', '"A"," ","B"')
        result = importer.parse((HEADER + "\n" + row).encode("latin-1"), "user-1")
        self.assertEqual("A B", result[0].description)

    def test_parse_when_quoted_comma_then_single_field(self) -> None:
        row = ROW.replace("Albert Heijn", "Janssen, B.V.")
        result = importer.parse((HEADER + "\n" + row).encode("latin-1"), "user-1")
        self.assertEqual("Janssen, B.V.", result[0].name_other_party)

    def test_parse_when_malformed_date_then_raises(self) -> None:
        row = ROW.replace("2026-01-03", "not-a-date", 1)
        with self.assertRaises(ValueError):
            importer.parse((HEADER + "\n" + row).encode("latin-1"), "user-1")


class ImporterProcessTests(BudgetDbTestCase):
    def test_process_when_duplicate_rows_then_only_new_rows_inserted(self) -> None:
        content = (HEADER + "\n" + ROW).encode("latin-1")
        first = importer.process(content, "user-1")
        second = importer.process(content, "user-1")
        self.assertEqual(date(2026, 1, 3), first)
        self.assertEqual(date(2026, 1, 3), second)
        self.assertEqual(1, Transaction.objects.filter(user_id="user-1").count())

    def test_process_when_multiple_rows_then_returns_max_date(self) -> None:
        row2 = ROW.replace("2026-01-03", "2026-02-05").replace(
            "000000000000000001", "000000000000000002"
        )
        max_date = importer.process((HEADER + "\n" + ROW + "\n" + row2).encode("latin-1"), "user-1")
        self.assertEqual(date(2026, 2, 5), max_date)

    def test_process_when_only_header_then_none_and_no_rows(self) -> None:
        self.assertIsNone(importer.process(HEADER.encode("latin-1"), "user-1"))
        self.assertFalse(Transaction.objects.exists())
