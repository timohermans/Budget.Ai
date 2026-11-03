"""Rabobank CSV importer, ported from RabobankCsvImporter.cs."""

from __future__ import annotations

import csv
import io
import logging
from datetime import date
from decimal import Decimal

from apps.merchants.normalizer import normalize
from apps.transactions.models import Transaction

logger = logging.getLogger(__name__)

REQUIRED_COLUMNS = (
    "Datum",
    "Bedrag",
    "Munt",
    "Omschrijving-1",
    "Omschrijving-2",
    "Omschrijving-3",
    "Volgnr",
    "Code",
    "IBAN/BBAN",
    "Tegenrekening IBAN/BBAN",
    "Naam tegenpartij",
)


def parse(content: bytes, user_id: str) -> list[Transaction]:
    """Parse a latin-1 Rabobank CSV without persisting it."""
    text = content.decode("latin-1")
    reader = csv.reader(io.StringIO(text, newline=""))
    rows = [row for row in reader if row]
    if len(rows) < 2:
        return []

    header = rows[0]
    column_index = {name: index for index, name in enumerate(header)}
    missing = [name for name in REQUIRED_COLUMNS if name not in column_index]
    if missing:
        raise ValueError(f"Missing Rabobank CSV columns: {', '.join(missing)}")

    def value(row: list[str], column: str) -> str:
        return row[column_index[column]]

    transactions: list[Transaction] = []
    for row in rows[1:]:
        transaction_date = date.fromisoformat(value(row, "Datum"))
        amount = Decimal(value(row, "Bedrag").replace(",", "."))
        description = (
            value(row, "Omschrijving-1")
            + value(row, "Omschrijving-2")
            + value(row, "Omschrijving-3")
        ).strip()
        name_other_party = value(row, "Naam tegenpartij")

        transactions.append(
            Transaction(
                date=transaction_date,
                user_id=user_id,
                amount=amount,
                currency=value(row, "Munt"),
                description=description,
                follow_number=int(value(row, "Volgnr")),
                code=value(row, "Code"),
                iban=value(row, "IBAN/BBAN"),
                iban_other_party=value(row, "Tegenrekening IBAN/BBAN"),
                name_other_party=name_other_party,
                name_other_party_normalized=normalize(name_other_party),
            )
        )

    return transactions


def process(content: bytes, user_id: str) -> date | None:
    """Insert rows that are not already present for the user and return the max date."""
    rows = parse(content, user_id)
    if not rows:
        logger.warning("Rabobank CSV for user %s contained no transaction rows", user_id)
        return None

    max_date = max(transaction.date for transaction in rows)
    existing = set(Transaction.objects.filter(user_id=user_id).values_list("iban", "follow_number"))
    to_add = [
        transaction
        for transaction in rows
        if (transaction.iban, transaction.follow_number) not in existing
    ]

    if to_add:
        Transaction.objects.bulk_create(to_add)

    logger.info(
        "Parsed %s transactions for user %s: %s inserted, %s duplicates skipped",
        len(rows),
        user_id,
        len(to_add),
        len(rows) - len(to_add),
    )
    return max_date
