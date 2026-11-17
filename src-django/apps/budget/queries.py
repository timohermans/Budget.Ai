"""Queries shared by the overview and toggle endpoints."""

from __future__ import annotations

from datetime import date

from django.db.models import Count

from apps.budget.services import TransactionOverview
from apps.merchants.models import Merchant, MerchantAlias
from apps.transactions.models import Transaction


def get_own_ibans(user_id: str) -> list[str]:
    """Return the user's IBANs ordered by transaction count descending."""
    counts = (
        Transaction.objects.filter(user_id=user_id)
        .values("iban")
        .annotate(count=Count("id"))
        .order_by("-count")
    )
    return [item["iban"] for item in counts]


def get_transaction_overview(user_id: str, start: date, end: date) -> list[TransactionOverview]:
    """Load the month window and resolve canonical merchant display data."""
    transactions = list(
        Transaction.objects.filter(user_id=user_id, date__gte=start, date__lt=end).order_by(
            "-date", "-name_other_party"
        )
    )
    merchants = {merchant.name_normalized: merchant for merchant in Merchant.objects.all()}
    aliases = {
        alias.name_normalized: alias.merchant
        for alias in MerchantAlias.objects.select_related("merchant").all()
    }

    results: list[TransactionOverview] = []
    for transaction in transactions:
        direct = merchants.get(transaction.name_other_party_normalized)
        alias_merchant = aliases.get(transaction.name_other_party_normalized)
        if direct is not None:
            display_name = direct.display_name
            logo_url = direct.logo_url
        elif alias_merchant is not None:
            display_name = alias_merchant.display_name
            logo_url = alias_merchant.logo_url
        else:
            display_name = None
            logo_url = None
        results.append(
            TransactionOverview(
                transaction=transaction,
                display_name=display_name,
                logo_url=logo_url,
            )
        )
    return results
