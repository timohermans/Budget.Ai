from __future__ import annotations

from datetime import date, timedelta

from django.test import Client
from django.utils import timezone

from apps.merchants import queries
from apps.merchants.models import Merchant, MerchantAlias, MerchantStatus
from apps.merchants.normalizer import normalize
from tests.base import BudgetDbTestCase, make_transaction


class MerchantQueryTests(BudgetDbTestCase):
    def add(self, name: str, day: date, follow: int = 1, user: str = "user-1") -> None:
        make_transaction(
            name_other_party=name,
            name_other_party_normalized=normalize(name),
            date=day,
            follow_number=follow,
            user_id=user,
        ).save()

    def test_rows_when_empty(self) -> None:
        self.assertEqual([], queries.get_rows())

    def test_rows_unmapped_first_ordered_by_count_desc(self) -> None:
        self.add("Albert Heijn", date(2026, 1, 3), 1)
        self.add("Albert Heijn", date(2026, 1, 10), 2)
        self.add("Jumbo", date(2026, 1, 5), 3)
        self.add("AH", date(2026, 1, 7), 4)

        rows = queries.get_rows()
        self.assertEqual(3, len(rows))
        self.assertEqual("albert heijn", rows[0].name_normalized)
        self.assertEqual(2, rows[0].transaction_count)
        self.assertEqual(date(2026, 1, 3), rows[0].first_seen)
        self.assertIsNone(rows[0].status)
        self.assertFalse(rows[0].is_linked)
        self.assertEqual("Albert Heijn", rows[0].raw_name)

    def test_rows_linked_shows_target(self) -> None:
        self.add("Albert Heijn", date(2026, 1, 3), 1)
        self.add("AH", date(2026, 1, 5), 2)
        merchant = Merchant.objects.create(
            name_normalized="albert heijn",
            display_name="Albert Heijn",
            logo_url="https://example.com/ah.png",
            status=MerchantStatus.MAPPED,
            updated_at=timezone.now(),
        )
        MerchantAlias.objects.create(
            name_normalized="ah", merchant=merchant, created_at=timezone.now()
        )

        row = next(item for item in queries.get_rows() if item.name_normalized == "ah")
        self.assertTrue(row.is_linked)
        self.assertEqual("Albert Heijn", row.linked_to_name)
        self.assertEqual(MerchantStatus.MAPPED, row.status)
        self.assertEqual("https://example.com/ah.png", row.logo_url)
        self.assertEqual("Albert Heijn", row.display_name)

    def test_rows_search_filters(self) -> None:
        self.add("Albert Heijn", date(2026, 1, 3), 1)
        self.add("Jumbo", date(2026, 1, 5), 2)
        rows = queries.get_rows(search="jumbo")
        self.assertEqual(["jumbo"], [row.name_normalized for row in rows])

    def test_rows_sort_count_asc(self) -> None:
        self.add("Albert Heijn", date(2026, 1, 3), 1)
        self.add("Albert Heijn", date(2026, 1, 10), 2)
        self.add("Jumbo", date(2026, 1, 5), 3)
        rows = queries.get_rows(sort="count", dir="asc")
        self.assertEqual("jumbo", rows[0].name_normalized)

    def test_options_recent_five_first(self) -> None:
        base = timezone.now() - timedelta(hours=10)
        for number in range(1, 9):
            Merchant.objects.create(
                name_normalized=f"shop{number:02d}",
                display_name=f"Shop {number:02d}",
                logo_url=None,
                status=MerchantStatus.MAPPED if number % 2 == 0 else MerchantStatus.NONE,
                updated_at=base + timedelta(minutes=number),
            )
        self.add("shop02", date(2026, 1, 4), 2)
        options = queries.get_options(None)
        self.assertEqual(5, len(options))
        self.assertEqual("shop08", options[0].name_normalized)
        self.assertEqual("shop07", options[1].name_normalized)
        self.assertEqual("shop04", options[4].name_normalized)

    def test_options_alias_recency_beats_updated_at(self) -> None:
        base = timezone.now() - timedelta(hours=10)
        recently_mapped = Merchant.objects.create(
            name_normalized="hema",
            display_name="HEMA",
            logo_url=None,
            status=MerchantStatus.MAPPED,
            updated_at=base + timedelta(minutes=9),
        )
        recently_aliased = Merchant.objects.create(
            name_normalized="albert heijn",
            display_name="Albert Heijn",
            logo_url=None,
            status=MerchantStatus.MAPPED,
            updated_at=base + timedelta(minutes=1),
        )
        MerchantAlias.objects.create(
            name_normalized="ah",
            merchant=recently_aliased,
            created_at=base + timedelta(minutes=10),
        )
        options = queries.get_options(None)
        self.assertEqual("albert heijn", options[0].name_normalized)
        self.assertNotEqual(recently_mapped.name_normalized, options[0].name_normalized)

    def test_options_fold_alias_counts(self) -> None:
        merchant = Merchant.objects.create(
            name_normalized="albert heijn",
            display_name="Albert Heijn",
            logo_url=None,
            status=MerchantStatus.MAPPED,
            updated_at=timezone.now(),
        )
        MerchantAlias.objects.create(
            name_normalized="ah", merchant=merchant, created_at=timezone.now()
        )
        self.add("albert heijn", date(2026, 1, 3), 1)
        self.add("AH", date(2026, 1, 5), 2)

        option = queries.get_options("albert")[0]
        self.assertEqual("Albert Heijn", option.label)
        self.assertEqual(2, option.total_transactions)

    def test_options_query_filters(self) -> None:
        Merchant.objects.create(
            name_normalized="jumbo",
            display_name="Jumbo",
            logo_url=None,
            status=MerchantStatus.MAPPED,
            updated_at=timezone.now(),
        )
        Merchant.objects.create(
            name_normalized="hema",
            display_name="HEMA",
            logo_url=None,
            status=MerchantStatus.MAPPED,
            updated_at=timezone.now(),
        )
        options = queries.get_options("jumbo")
        self.assertEqual(1, len(options))
        self.assertEqual("jumbo", options[0].name_normalized)


class MerchantViewTests(BudgetDbTestCase):
    def make_client(self) -> Client:
        return Client(HTTP_X_TEST_USER="user-1")

    def add(self, name: str, follow: int = 1) -> None:
        make_transaction(
            name_other_party=name,
            name_other_party_normalized=normalize(name),
            follow_number=follow,
            date=date(2026, 1, 3),
        ).save()

    def test_map_creates_merchant(self) -> None:
        self.add("Albert Heijn")
        response = self.make_client().post(
            "/merchants/map",
            {
                "name": "Albert Heijn",
                "displayName": "Albert Heijn",
                "logoUrl": "https://example.com/ah.png",
            },
        )
        self.assertEqual(200, response.status_code)
        merchant = Merchant.objects.get()
        self.assertEqual("albert heijn", merchant.name_normalized)
        self.assertEqual("https://example.com/ah.png", merchant.logo_url)
        self.assertEqual(MerchantStatus.MAPPED, merchant.status)

    def test_map_updates_existing(self) -> None:
        self.add("Albert Heijn")
        client = self.make_client()
        client.post(
            "/merchants/map",
            {
                "name": "Albert Heijn",
                "displayName": "Old",
                "logoUrl": "https://example.com/old.png",
            },
        )
        client.post(
            "/merchants/map",
            {
                "name": "ALBERT HEIJN",
                "displayName": "New",
                "logoUrl": "https://example.com/new.png",
            },
        )
        merchant = Merchant.objects.get()
        self.assertEqual("New", merchant.display_name)
        self.assertEqual("https://example.com/new.png", merchant.logo_url)

    def test_map_invalid_url_does_nothing(self) -> None:
        self.add("Albert Heijn")
        self.make_client().post(
            "/merchants/map",
            {
                "name": "Albert Heijn",
                "displayName": "Albert Heijn",
                "logoUrl": "not-a-url",
            },
        )
        self.assertFalse(Merchant.objects.exists())

    def test_link_creates_alias(self) -> None:
        self.add("Albert Heijn", 1)
        self.add("AH", 2)
        client = self.make_client()
        client.post(
            "/merchants/map",
            {
                "name": "Albert Heijn",
                "displayName": "Albert Heijn",
                "logoUrl": "https://example.com/ah.png",
            },
        )
        client.post("/merchants/link", {"name": "AH", "merchantName": "Albert Heijn"})
        alias = MerchantAlias.objects.get()
        self.assertEqual("ah", alias.name_normalized)
        self.assertEqual(Merchant.objects.get(), alias.merchant)

    def test_link_missing_target_does_nothing(self) -> None:
        self.add("AH")
        self.make_client().post("/merchants/link", {"name": "AH", "merchantName": "Albert Heijn"})
        self.assertFalse(MerchantAlias.objects.exists())

    def test_clear_alias_keeps_merchant(self) -> None:
        self.add("Albert Heijn", 1)
        self.add("AH", 2)
        client = self.make_client()
        client.post(
            "/merchants/map",
            {
                "name": "Albert Heijn",
                "displayName": "Albert Heijn",
                "logoUrl": "https://example.com/ah.png",
            },
        )
        client.post("/merchants/link", {"name": "AH", "merchantName": "Albert Heijn"})
        client.post("/merchants/clear", {"name": "AH"})
        self.assertFalse(MerchantAlias.objects.exists())
        self.assertEqual(1, Merchant.objects.count())

    def test_clear_merchant_removes_aliases(self) -> None:
        self.add("Albert Heijn", 1)
        self.add("AH", 2)
        client = self.make_client()
        client.post(
            "/merchants/map",
            {
                "name": "Albert Heijn",
                "displayName": "Albert Heijn",
                "logoUrl": "https://example.com/ah.png",
            },
        )
        client.post("/merchants/link", {"name": "AH", "merchantName": "Albert Heijn"})
        client.post("/merchants/clear", {"name": "Albert Heijn"})
        self.assertFalse(Merchant.objects.exists())
        self.assertFalse(MerchantAlias.objects.exists())
