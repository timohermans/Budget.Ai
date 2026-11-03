"""Transaction URLs."""

from __future__ import annotations

from django.urls import path

from apps.transactions import views

app_name = "transactions"

urlpatterns = [
    path("transactions/upload", views.upload, name="upload"),
    path("transactions/toggle-fixed", views.toggle_fixed, name="toggle_fixed"),
]
