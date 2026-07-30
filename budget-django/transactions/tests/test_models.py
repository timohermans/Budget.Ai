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
