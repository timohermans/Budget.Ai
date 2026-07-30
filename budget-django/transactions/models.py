from typing import ClassVar

from django.contrib.auth import get_user_model
from django.db import models

from .managers import TransactionManager

User = get_user_model()


class Transaction(models.Model):
    objects: ClassVar[TransactionManager] = TransactionManager() # pyright: ignore[reportIncompatibleVariableOverride]

    follow_number = models.IntegerField(null=False, blank=False)
    iban = models.CharField(null=False, blank=False)
    currency = models.CharField(null=True, blank=True, default="EUR")
    amount = models.DecimalField(
        null=False, blank=False, max_digits=10, decimal_places=2
    )
    date = models.DateField(null=False, blank=False)
    name_other_party = models.CharField(null=False, blank=False)
    iban_other_party = models.CharField(null=False, blank=False)
    description = models.TextField(null=True, blank=True)
    is_not_fixed = models.BooleanField(default=False)
    code = models.CharField(null=False, blank=False)
    user = models.ForeignKey(User, on_delete=models.CASCADE, null=False, blank=False)

    class Meta:
        constraints = [
            models.UniqueConstraint(
                fields=["iban", "follow_number", "user"], name="unique_transaction"
            )
        ]

    def is_fixed(self, my_ibans: list[str]) -> bool:  # TODO: unit test
        if self.is_not_fixed:
            return False
        if self.is_income() and self.is_from_own_account(my_ibans):
            return False
        if "paypal" in self.name_other_party.lower():
            return False
        if self.code == "db" and self.description is not None and "sparen" in self.description.lower():
            return True
        if self.code == "db" and "Rabobank" == self.name_other_party:
            return True
        if self.code in ("sb", "cb", "bg", "ei", "tb"):
            return True
        return False

    def is_variable(self, my_ibans: list[str]):
        return not self.is_fixed(my_ibans)

    def is_expense(self):
        return self.amount < 0

    def is_income(self):
        return not self.is_expense()

    def toggle_fixed(self):
        self.is_not_fixed = not self.is_not_fixed
        self.save()

    def is_from_own_account(self, my_ibans: list[str]):
        return self.iban_other_party in my_ibans

    def __str__(self) -> str:
        return f"{self.amount} -> {self.iban} <- {self.iban_other_party} ({self.name_other_party})"
