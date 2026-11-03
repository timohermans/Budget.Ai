from __future__ import annotations

from django.apps import AppConfig


class MerchantsConfig(AppConfig):
    default_auto_field = "django.db.models.AutoField"
    name = "apps.merchants"
    label = "merchants"
