import datetime
from decimal import Decimal
from io import StringIO

from django.contrib.auth import get_user_model
from django.core.files.uploadedfile import SimpleUploadedFile
from django.test import TestCase

from transactions.models import Transaction

User = get_user_model()


class TransactionManagerTestCase(TestCase):
    @classmethod
    def setUpTestData(cls):
        User.objects.create(username='test', email='', password='')

    @staticmethod
    def _to_uploaded_file(csv_string: str):
        csv_data = StringIO(csv_string.strip())
        mock_file = SimpleUploadedFile(
            "test_transactions.csv",
            csv_data.read().encode(),
            content_type="text/csv"
        )
        return mock_file

    def test_process_file__when_only_header__succeeds(self):
        user = User.objects.get(username='test')
        mock_file = self._to_uploaded_file("""
"IBAN/BBAN","Munt","BIC","Volgnr","Datum","Rentedatum","Bedrag","Saldo na trn","Tegenrekening IBAN/BBAN","Naam tegenpartij","Naam uiteindelijke partij","Naam initiÎrende partij","BIC tegenpartij","Code","Batch ID","Transactiereferentie","Machtigingskenmerk","Incassant ID","Betalingskenmerk","Omschrijving-1","Omschrijving-2","Omschrijving-3","Reden retour","Oorspr bedrag","Oorspr munt","Koers"
        """)

        result = Transaction.objects.process_file(mock_file, user)

        self.assertEqual(0, result)
        self.assertEqual(0, len(Transaction.objects.all()))

    def test_process_file__when_single_line__succeeds(self):
        csv_string = """
"IBAN/BBAN","Munt","BIC","Volgnr","Datum","Rentedatum","Bedrag","Saldo na trn","Tegenrekening IBAN/BBAN","Naam tegenpartij","Naam uiteindelijke partij","Naam initiÎrende partij","BIC tegenpartij","Code","Batch ID","Transactiereferentie","Machtigingskenmerk","Incassant ID","Betalingskenmerk","Omschrijving-1","Omschrijving-2","Omschrijving-3","Reden retour","Oorspr bedrag","Oorspr munt","Koers"
"OWNED1","EUR","RABONL2U","000000000000011111","2026-04-15","2026-04-15","-24,66","+5555,27","THEIRS01","Shop","","","RABONLLLXXX","ie","0000000000000000","","","","","Payment"," ","","","","",""
        """
        mock_file = self._to_uploaded_file(csv_string)
        user = User.objects.get(username='test')

        Transaction.objects.process_file(mock_file, user)

        transactions = Transaction.objects.all()
        self.assertEqual(1, len(transactions))
        transaction = transactions[0]
        self.assertEqual('OWNED1', transaction.iban)
        self.assertEqual('EUR', transaction.currency)
        self.assertEqual(datetime.date(2026, 4, 15), transaction.date)
        self.assertEqual(Decimal('-24.66'), transaction.amount)
        self.assertEqual('THEIRS01', transaction.iban_other_party)
        self.assertEqual('Shop', transaction.name_other_party)
        self.assertEqual('ie', transaction.code)
        self.assertEqual('Payment', transaction.description)

    def test_process_file__when_inserting_duplicates__ignores_duplicate(self):
        user = User.objects.get(username='test')
        csv_string = """
"IBAN/BBAN","Munt","BIC","Volgnr","Datum","Rentedatum","Bedrag","Saldo na trn","Tegenrekening IBAN/BBAN","Naam tegenpartij","Naam uiteindelijke partij","Naam initiÎrende partij","BIC tegenpartij","Code","Batch ID","Transactiereferentie","Machtigingskenmerk","Incassant ID","Betalingskenmerk","Omschrijving-1","Omschrijving-2","Omschrijving-3","Reden retour","Oorspr bedrag","Oorspr munt","Koers"
"OWNED1","EUR","RABONL2U","000000000000011111","2026-04-15","2026-04-15","-24,66","+5555,27","THEIRS01","Shop","","","RABONLLLXXX","ie","0000000000000000","","","","","Payment"," ","","","","",""
        """
        file_1 = self._to_uploaded_file(csv_string)
        file_2 = self._to_uploaded_file(csv_string)
        Transaction.objects.process_file(file_1, user)

        # act
        Transaction.objects.process_file(file_2, user)

        self.assertEqual(1, len(Transaction.objects.all()))

    def test_get_summary_for__succeeds(self):
        user = User.objects.get(username='test')

        Transaction.objects.bulk_create([
            Transaction(
                follow_number=1,
                amount=3000.3,
                date=datetime.date(2025, 12, 12),
                iban='OWNED01',
                iban_other_party='WORK01',
                name_other_party='Werkgever A',
                code='sb',
                description='Salaris werkgever A',
                user=user,
            ),
            Transaction(
                follow_number=2,
                amount=2000.3,
                date=datetime.date(2025, 12, 12),
                iban='OWNED01',
                iban_other_party='WORK02',
                name_other_party='Werkgever B',
                code='sb',
                description='Salaris werkgever B',
                user=user,
            ),
            Transaction(
                follow_number=3,
                amount=-44,
                date=datetime.date(2025, 12, 8),
                iban='OWNED01',
                iban_other_party='HOBBY01',
                name_other_party='Piano lerares',
                code='bg',
                description='Lessen Timo',
                user=user,
            ),
            Transaction(
                follow_number=4,
                amount=-51.03,
                date=datetime.date(2025, 12, 6),
                iban='OWNED01',
                iban_other_party='CORPO01',
                name_other_party='ODIDO Netherlands',
                code='ei',
                description='Mob 0611111111 Klantnr. 1.1231241',
                user=user,
            ),
            Transaction(
                follow_number=5,
                amount=-109,
                date=datetime.date(2025, 12, 6),
                iban='OWNED01',
                iban_other_party='CORPO02',
                name_other_party='ESSENT RETAIL ENERGIE B.V.',
                code='cb',
                description='150046212311/KLANT 1235467 KNMRK',
                user=user,
            ),
            Transaction(
                follow_number=6,
                amount=-5.45,
                date=datetime.date(2025, 12, 2),
                iban='OWNED01',
                iban_other_party='CORPO03',
                name_other_party='Rabobank',
                code='db',
                description='Kosten basispakket',
                user=user,
            ),
            Transaction(
                follow_number=7,
                amount=-1801.81,
                date=datetime.date(2025, 12, 28),
                iban='OWNED01',
                iban_other_party='CORPO04',
                name_other_party='BLG Wonen',
                code='ei',
                description='Hypotheek termijnbetaling.',
                user=user,
            ),
            # variabele uitgaven
            # week 1
            Transaction(
                follow_number=8,
                amount=-20.72,
                date=datetime.date(2026, 1, 2),
                iban='OWNED01',
                iban_other_party='SHOP01',
                name_other_party='AH - Jan Linders 4141',
                code='bc',
                description='Terminal: Boodschappen 1',
                user=user,
            ),
            Transaction(
                follow_number=9,
                amount=-300,
                date=datetime.date(2026, 1, 2),
                iban='OWNED01',
                iban_other_party='SHOP01',
                name_other_party='AH - Jan Linders 4141',
                code='bc',
                description='Terminal: Boodschappen 1',
                user=user,
            ),
            Transaction(
                follow_number=10,
                amount=-800,
                date=datetime.date(2026, 1, 11),
                iban='OWNED01',
                iban_other_party='SHOP01',
                name_other_party='AH - Jan Linders 4141',
                code='bc',
                description='Terminal: Boodschappen 2',
                user=user,
            ),
            Transaction(
                follow_number=11,
                amount=-1000,
                date=datetime.date(2026, 1, 11),
                iban='OWNED01',
                iban_other_party='OWNED02',
                name_other_party='Spaar',
                code='tb',
                description='Maandelijks sparen',
                user=user,
            ),
            Transaction(
                follow_number=12,
                amount=1000,
                date=datetime.date(2026, 1, 11),
                iban='OWNED02',
                iban_other_party='OWNED01',
                name_other_party='Betaalrekening',
                code='bc',
                description='Maandelijks sparen',
                user=user,
            ),
            Transaction(
                follow_number=13,
                amount=500,
                date=datetime.date(2026, 1, 11),
                iban='OWNED01',
                iban_other_party='OWNED02',
                name_other_party='Spaar',
                code='tb',
                description='Buffer geld',
                user=user,
            ),
            Transaction(
                follow_number=14,
                amount=-500,
                date=datetime.date(2026, 1, 11),
                iban='OWNED02',
                iban_other_party='OWNED01',
                name_other_party='Betaalrekening',
                code='bc',
                description='Buffer sparen',
                user=user,
            ),
        ])

        # act
        summary = Transaction.objects.get_summary_for(2026, 1, None, user)

        # arrange
        self.assertIsNotNone(summary)
        self.assertEqual(Decimal('5000.6'), summary.income)
        self.assertEqual(Decimal('2011.29'), summary.expenses)
        self.assertEqual(Decimal('1120.72'), summary.spent)
        self.assertEqual(Decimal('2989.31'), summary.budget)
        self.assertEqual(Decimal('1868.59'), summary.left)

