import datetime

from django.contrib.auth import get_user_model
from django.test import TestCase

from transactions.models import Transaction

User = get_user_model()


class TransactionTestCase(TestCase):
    def setUp(self):
        User.objects.create(username='test', email='', password='')

    def test_create__when_user_exists__succeeds(self):
        transaction = Transaction.objects.create(
            follow_number=1,
            iban='OWNED1',
            currency='EUR',
            amount=10,
            date=datetime.date.today(),
            name_other_party='shop',
            iban_other_party='THEIRS1',
            description='TEST',
            user=User.objects.get(username='test'),
            code='ei',
        )

        self.assertIsNotNone(transaction)


class TransactionIsFixedTestCase(TestCase):
    MY_IBANS = ['OWNED1', 'OWNED2']

    @staticmethod
    def _transaction(
        amount,
        code,
        name_other_party='Test Party',
        iban_other_party='THEIRS1',
        is_not_fixed=False,
        description=None,
    ):
        return Transaction(
            follow_number=1,
            iban='OWNED1',
            currency='EUR',
            amount=amount,
            date=datetime.date(2026, 1, 1),
            name_other_party=name_other_party,
            iban_other_party=iban_other_party,
            description=description,
            is_not_fixed=is_not_fixed,
            code=code,
        )

    def test_is_fixed__when_transaction_is_flagged_variable__returns_false(self):
        transaction = self._transaction(amount=-100, code='cb', is_not_fixed=True)

        result = transaction.is_fixed(self.MY_IBANS)

        self.assertFalse(result)

    def test_is_fixed__when_income_from_own_account__returns_false(self):
        transaction = self._transaction(
            amount=500, code='sb', name_other_party='Rabobank', iban_other_party='OWNED2'
        )

        result = transaction.is_fixed(self.MY_IBANS)

        self.assertFalse(result)

    def test_is_fixed__when_expense_from_own_account__returns_true(self):
        transaction = self._transaction(
            amount=-500, code='tb', name_other_party='Spaar', iban_other_party='OWNED2'
        )

        result = transaction.is_fixed(self.MY_IBANS)

        self.assertTrue(result)

    def test_is_fixed__when_counterparty_name_contains_paypal__returns_false(self):
        transaction = self._transaction(amount=-100, code='cb', name_other_party='PayPal EU')

        result = transaction.is_fixed(self.MY_IBANS)

        self.assertFalse(result)

    def test_is_fixed__when_code_db_and_description_contains_sparen__returns_true(self):
        transaction = self._transaction(
            amount=-1000, code='db', name_other_party='Spaar', description='Maandelijks sparen'
        )

        result = transaction.is_fixed(self.MY_IBANS)

        self.assertTrue(result)

    def test_is_fixed__when_code_db_and_counterparty_is_rabobank__returns_true(self):
        transaction = self._transaction(
            amount=-5.45, code='db', name_other_party='Rabobank', description='Kosten basispakket'
        )

        result = transaction.is_fixed(self.MY_IBANS)

        self.assertTrue(result)

    def test_is_fixed__when_code_is_fixed_code__returns_true(self):
        for code in ['sb', 'cb', 'bg', 'ei', 'tb']:
            with self.subTest(code=code):
                transaction = self._transaction(amount=-100, code=code)

                result = transaction.is_fixed(self.MY_IBANS)

                self.assertTrue(result)

    def test_is_fixed__when_no_rule_matches__returns_false(self):
        transaction = self._transaction(amount=-150, code='bc', name_other_party='Albert Heijn')

        result = transaction.is_fixed(self.MY_IBANS)

        self.assertFalse(result)

    def test_is_fixed__when_flagged_variable_overrides_fixed_code__returns_false(self):
        transaction = self._transaction(
            amount=-800, code='cb', name_other_party='Insurance Co', is_not_fixed=True
        )

        result = transaction.is_fixed(self.MY_IBANS)

        self.assertFalse(result)

    def test_is_fixed__when_income_from_own_account_overrides_fixed_code__returns_false(self):
        transaction = self._transaction(
            amount=500, code='sb', name_other_party='Rabobank', iban_other_party='OWNED2'
        )

        result = transaction.is_fixed(self.MY_IBANS)

        self.assertFalse(result)

    def test_is_variable__when_transaction_is_fixed__returns_false(self):
        transaction = self._transaction(amount=-100, code='cb')

        result = transaction.is_variable(self.MY_IBANS)

        self.assertFalse(result)

    def test_is_variable__when_transaction_is_variable__returns_true(self):
        transaction = self._transaction(amount=-100, code='bc')

        result = transaction.is_variable(self.MY_IBANS)

        self.assertTrue(result)
