from __future__ import annotations

from django.apps import AppConfig


class TransactionsConfig(AppConfig):
    default_auto_field = "django.db.models.AutoField"
    name = "apps.transactions"
    label = "transactions"
