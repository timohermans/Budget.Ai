"""Shared merchant queries for the admin page and htmx partials."""

from __future__ import annotations

from collections import Counter
from dataclasses import dataclass, field
from datetime import date, datetime

from apps.merchants.models import Merchant, MerchantAlias, MerchantStatus
from apps.transactions.models import Transaction


@dataclass(slots=True)
class MerchantRow:
    name_normalized: str
    raw_name: str
    transaction_count: int
    first_seen: date
    status: int | None
    display_name: str | None
    logo_url: str | None
    linked_to_name: str | None

    @property
    def is_linked(self) -> bool:
        return self.linked_to_name is not None

    @property
    def is_unmapped(self) -> bool:
        return self.status is None

    @property
    def is_no_logo(self) -> bool:
        return self.status == MerchantStatus.NONE

    @property
    def is_mapped(self) -> bool:
        return self.status is not None and self.status != MerchantStatus.NONE


@dataclass(slots=True)
class MerchantOption:
    name_normalized: str
    label: str
    total_transactions: int
    last_linked: datetime


@dataclass(slots=True)
class RowsPartial:
    search: str
    sort: str
    dir: str
    rows: list[MerchantRow] = field(default_factory=list)
    recent_options: list[MerchantOption] = field(default_factory=list)


@dataclass(slots=True)
class OptionsPartial:
    name: str
    q: str
    search: str
    sort: str
    dir: str
    options: list[MerchantOption] = field(default_factory=list)


def _rank(row: MerchantRow) -> int:
    if row.is_linked:
        return 1
    if row.status is None:
        return 0
    if row.status == MerchantStatus.NONE:
        return 2
    return 3


def get_rows(search: str = "", sort: str = "", dir: str = "") -> list[MerchantRow]:
    transactions = list(Transaction.objects.exclude(name_other_party_normalized="").order_by("id"))
    merchants = {merchant.name_normalized: merchant for merchant in Merchant.objects.all()}
    aliases = {
        alias.name_normalized: alias
        for alias in MerchantAlias.objects.select_related("merchant").all()
    }

    grouped: dict[str, list[Transaction]] = {}
    for transaction in transactions:
        grouped.setdefault(transaction.name_other_party_normalized, []).append(transaction)

    rows: list[MerchantRow] = []
    for key, group in grouped.items():
        first = group[0]
        direct = merchants.get(key)
        alias = aliases.get(key)
        alias_merchant = alias.merchant if alias is not None else None

        status = (
            direct.status
            if direct is not None
            else alias_merchant.status
            if alias_merchant is not None
            else None
        )
        display_name = (
            direct.display_name
            if direct is not None
            else alias_merchant.display_name
            if alias_merchant is not None
            else None
        )
        logo_url = (
            direct.logo_url
            if direct is not None
            else alias_merchant.logo_url
            if alias_merchant is not None
            else None
        )

        rows.append(
            MerchantRow(
                name_normalized=key,
                raw_name=first.name_other_party,
                transaction_count=len(group),
                first_seen=min(item.date for item in group),
                status=status,
                display_name=display_name,
                logo_url=logo_url,
                linked_to_name=alias_merchant.display_name if alias_merchant is not None else None,
            )
        )

    query = search.strip().lower()
    if query:
        rows = [
            row
            for row in rows
            if query in row.name_normalized.lower() or query in row.raw_name.lower()
        ]

    is_desc = dir == "desc"
    if sort == "name":
        rows.sort(key=lambda row: row.name_normalized, reverse=is_desc)
    elif sort == "count":
        rows.sort(
            key=lambda row: (
                -row.transaction_count if is_desc else row.transaction_count,
                row.name_normalized,
            )
        )
    elif sort == "firstSeen":
        rows.sort(
            key=lambda row: (
                -row.first_seen.toordinal() if is_desc else row.first_seen.toordinal(),
                row.name_normalized,
            )
        )
    elif sort == "status":
        rows.sort(
            key=lambda row: (
                -_rank(row) if is_desc else _rank(row),
                row.name_normalized,
            )
        )
    else:
        rows.sort(
            key=lambda row: (
                _rank(row),
                -row.transaction_count if _rank(row) == 0 else 0,
                row.name_normalized,
            )
        )

    return rows


def get_options(q: str | None = None) -> list[MerchantOption]:
    query = (q or "").strip().lower()

    transaction_counts = Counter(
        Transaction.objects.values_list("name_other_party_normalized", flat=True)
    )

    merchants = Merchant.objects.prefetch_related("aliases").all()
    if query:
        merchants = merchants.filter(display_name__icontains=query)

    options: list[MerchantOption] = []
    for merchant in merchants:
        aliases = list(merchant.aliases.all())
        alias_names = [alias.name_normalized for alias in aliases]
        latest_alias = max((alias.created_at for alias in aliases), default=None)
        last_linked = merchant.updated_at
        if latest_alias is not None and latest_alias > last_linked:
            last_linked = latest_alias

        total = transaction_counts.get(merchant.name_normalized, 0) + sum(
            transaction_counts.get(name, 0) for name in alias_names
        )
        options.append(
            MerchantOption(
                name_normalized=merchant.name_normalized,
                label=merchant.display_name,
                total_transactions=total,
                last_linked=last_linked,
            )
        )

    options.sort(key=lambda option: option.name_normalized)
    options.sort(key=lambda option: option.last_linked, reverse=True)
    return options[:5]


def build_rows_partial(search: str, sort: str, dir: str) -> RowsPartial:
    return RowsPartial(
        search=search,
        sort=sort,
        dir=dir,
        rows=get_rows(search, sort, dir),
        recent_options=get_options(None),
    )
