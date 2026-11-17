"""Django models for the Budget domain.

The table and column names match the historical EF Core schema, so an existing
database can be adopted once with ``migrate --fake-initial``. Django now owns
the schema through the migrations in ``apps/transactions/migrations``.
"""

from __future__ import annotations

from collections.abc import Collection
from decimal import Decimal

from django.db import models


class Transaction(models.Model):
    id = models.AutoField(primary_key=True, db_column="id")
    follow_number = models.IntegerField(db_column="follow_number")
    iban = models.TextField(db_column="iban")
    currency = models.TextField(null=True, blank=True, db_column="currency")
    amount = models.DecimalField(max_digits=10, decimal_places=2, db_column="amount")
    date = models.DateField(db_column="date")
    name_other_party = models.TextField(db_column="name_other_party")
    name_other_party_normalized = models.TextField(db_column="name_other_party_normalized")
    iban_other_party = models.TextField(db_column="iban_other_party")
    description = models.TextField(null=True, blank=True, db_column="description")
    is_not_fixed = models.BooleanField(default=False, db_column="is_not_fixed")
    code = models.TextField(db_column="code")
    user_id = models.TextField(db_column="user_id")

    class Meta:
        managed = True
        db_table = "transactions"
        ordering = ("-date", "-name_other_party")
        constraints = [
            models.UniqueConstraint(
                fields=["iban", "follow_number", "user_id"],
                name="ix_transactions_iban_follow_number_user_id",
            ),
        ]

    def __str__(self) -> str:
        return f"{self.date} {self.name_other_party} {self.amount}"

    def is_expense(self) -> bool:
        return self.amount < Decimal("0")

    def is_income(self) -> bool:
        return not self.is_expense()

    def is_from_own_account(self, own_ibans: Collection[str]) -> bool:
        return self.iban_other_party in own_ibans
