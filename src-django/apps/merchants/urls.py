"""Merchant administration URLs."""

from __future__ import annotations

from django.urls import path

from apps.merchants import views

app_name = "merchants"

urlpatterns = [
    path("merchants", views.index, name="index"),
    path("merchants/", views.index, name="index_slash"),
    path("merchants/rows", views.rows, name="rows"),
    path("merchants/options", views.options, name="options"),
    path("merchants/map", views.map_merchant, name="map"),
    path("merchants/link", views.link_merchant, name="link"),
    path("merchants/clear", views.clear_merchant, name="clear"),
]
