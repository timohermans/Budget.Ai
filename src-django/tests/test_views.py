from __future__ import annotations

from datetime import date

from django.core.files.uploadedfile import SimpleUploadedFile
from django.test import Client, override_settings

from apps.transactions.models import Transaction
from tests.base import BudgetDbTestCase, make_transaction

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


@override_settings(TEST_MODE=True)
class UploadViewTests(BudgetDbTestCase):
    def make_client(self) -> Client:
        return Client(enforce_csrf_checks=True, HTTP_X_TEST_USER="user-1")

    def upload(self, content: str) -> object:
        uploaded = SimpleUploadedFile(
            "rabo.csv", content.encode("latin-1"), content_type="text/csv"
        )
        return self.make_client().post("/transactions/upload", {"file": uploaded})

    def test_upload_valid_redirects_to_latest_month(self) -> None:
        response = self.upload(HEADER + "\n" + ROW)
        self.assertEqual(302, response.status_code)
        self.assertEqual("/budget/2026/1", response["Location"])

    def test_upload_valid_imports_for_user_without_csrf_token(self) -> None:
        self.upload(HEADER + "\n" + ROW)
        stored = Transaction.objects.get()
        self.assertEqual("user-1", stored.user_id)
        self.assertEqual("Albert Heijn", stored.name_other_party)

    def test_upload_header_only_redirects_to_budget(self) -> None:
        response = self.upload(HEADER)
        self.assertEqual(302, response.status_code)
        self.assertEqual("/budget", response["Location"])
        self.assertFalse(Transaction.objects.exists())

    def test_upload_malformed_renders_error(self) -> None:
        response = self.upload((HEADER + "\n" + ROW).replace("2026-01-03", "not-a-date", 1))
        self.assertEqual(200, response.status_code)
        self.assertContains(response, "upload-error")
        self.assertFalse(Transaction.objects.exists())

    def test_upload_without_file_renders_error(self) -> None:
        response = self.make_client().post("/transactions/upload", {})
        self.assertEqual(200, response.status_code)
        self.assertContains(response, "upload-error")


@override_settings(TEST_MODE=True)
class ToggleFixedViewTests(BudgetDbTestCase):
    def make_client(self) -> Client:
        return Client(enforce_csrf_checks=True, HTTP_X_TEST_USER="user-1")

    def test_toggle_missing_returns_404(self) -> None:
        response = self.make_client().post("/transactions/toggle-fixed", {"id": 999})
        self.assertEqual(404, response.status_code)

    def test_toggle_other_user_returns_404_and_unchanged(self) -> None:
        transaction = make_transaction(user_id="user-2", code="ei")
        transaction.save()
        response = self.make_client().post("/transactions/toggle-fixed", {"id": transaction.id})
        self.assertEqual(404, response.status_code)
        transaction.refresh_from_db()
        self.assertFalse(transaction.is_not_fixed)

    def test_toggle_persists_and_returns_oob_fragment(self) -> None:
        transaction = make_transaction(code="ei", date=date(2026, 10, 5))
        transaction.save()
        response = self.make_client().post("/transactions/toggle-fixed", {"id": transaction.id})
        self.assertEqual(200, response.status_code)
        self.assertContains(response, 'data-testid="toggle-submit"')
        self.assertContains(response, 'hx-swap-oob="true"')
        self.assertContains(response, 'data-testid="spent-week-')
        transaction.refresh_from_db()
        self.assertTrue(transaction.is_not_fixed)

    def test_toggle_twice_restores_flag(self) -> None:
        transaction = make_transaction(code="ei", date=date(2026, 10, 5))
        transaction.save()
        client = self.make_client()
        client.post("/transactions/toggle-fixed", {"id": transaction.id})
        client.post("/transactions/toggle-fixed", {"id": transaction.id})
        transaction.refresh_from_db()
        self.assertFalse(transaction.is_not_fixed)


@override_settings(TEST_MODE=True)
class OverviewViewTests(BudgetDbTestCase):
    def make_client(self) -> Client:
        return Client(HTTP_X_TEST_USER="user-1")

    def seed(self) -> None:
        make_transaction(
            amount=3000,
            date=date(2098, 12, 15),
            code="sb",
            name_other_party="Employer",
            name_other_party_normalized="employer",
            follow_number=1,
        ).save()
        make_transaction(
            amount=-800,
            date=date(2098, 12, 15),
            code="cb",
            name_other_party="Insurance Co",
            name_other_party_normalized="insurance co",
            follow_number=2,
        ).save()
        make_transaction(
            amount=-150,
            date=date(2099, 1, 3),
            code="bc",
            name_other_party="Albert Heijn",
            name_other_party_normalized="albert heijn",
            follow_number=3,
        ).save()

    def test_overview_renders_month_and_totals(self) -> None:
        self.seed()
        response = self.make_client().get("/budget/2099/1")
        self.assertEqual(200, response.status_code)
        self.assertContains(response, "January")
        self.assertContains(response, 'data-testid="budget-value">2200')
        self.assertContains(response, 'data-testid="spent-total">150')
        self.assertContains(response, 'data-testid="week-1"')

    def test_overview_fixed_query_renders_card(self) -> None:
        self.seed()
        response = self.make_client().get("/budget/2099/1?fixed=income")
        self.assertContains(response, 'data-testid="fixed-detail-card"')
        self.assertContains(response, "Employer")

    def test_overview_htmx_fixed_returns_partial_only(self) -> None:
        self.seed()
        response = self.make_client().get("/budget/2099/1?fixed=income", HTTP_HX_REQUEST="true")
        self.assertEqual(200, response.status_code)
        self.assertContains(response, 'data-testid="fixed-detail-card"')
        self.assertNotContains(response, "<!doctype html>")

    def test_overview_week_segment_expands_transactions(self) -> None:
        self.seed()
        response = self.make_client().get("/budget/2099/1/1")
        self.assertContains(response, 'data-testid="week-1-transactions"')
        self.assertContains(response, 'data-testid="name-other-party">Albert Heijn')

    def test_overview_sort_amount_desc(self) -> None:
        self.seed()
        make_transaction(
            amount=-50,
            date=date(2099, 1, 4),
            code="bc",
            name_other_party="Hema",
            name_other_party_normalized="hema",
            follow_number=4,
        ).save()
        response = self.make_client().get("/budget/2099/1/1?sort=hoogste-uitgaves")
        content = response.content.decode()
        albert = content.index("Albert Heijn")
        hema = content.index("Hema")
        self.assertLess(albert, hema)
