"""Django models for the global merchant and alias map."""

from __future__ import annotations

from django.db import models


class MerchantStatus(models.IntegerChoices):
    MAPPED = 0, "Mapped"
    NONE = 1, "None"


class Merchant(models.Model):
    id = models.AutoField(primary_key=True, db_column="id")
    name_normalized = models.TextField(db_column="name_normalized")
    display_name = models.TextField(db_column="display_name")
    logo_url = models.TextField(null=True, blank=True, db_column="logo_url")
    status = models.IntegerField(choices=MerchantStatus.choices, db_column="status")
    updated_at = models.DateTimeField(db_column="updated_at")

    class Meta:
        managed = True
        db_table = "merchants"
        constraints = [
            models.UniqueConstraint(
                fields=["name_normalized"],
                name="ix_merchants_name_normalized",
            ),
        ]

    def __str__(self) -> str:
        return self.display_name or self.name_normalized


class MerchantAlias(models.Model):
    id = models.AutoField(primary_key=True, db_column="id")
    name_normalized = models.TextField(db_column="name_normalized")
    merchant = models.ForeignKey(
        Merchant,
        on_delete=models.CASCADE,
        db_column="merchant_id",
        related_name="aliases",
    )
    created_at = models.DateTimeField(db_column="created_at")

    class Meta:
        managed = True
        db_table = "merchant_aliases"
        constraints = [
            models.UniqueConstraint(
                fields=["name_normalized"],
                name="ix_merchant_aliases_name_normalized",
            ),
        ]

    def __str__(self) -> str:
        return self.name_normalized
