"""Budget overview URLs."""

from __future__ import annotations

from django.urls import path

from apps.budget import views

app_name = "budget"

urlpatterns = [
    path("budget/", views.overview, name="overview_root"),
    path("budget", views.overview, name="overview_root_no_slash"),
    path("budget/<int:year>/<int:month>", views.overview, name="overview"),
    path("budget/<int:year>/<int:month>/", views.overview, name="overview_slash"),
    path(
        "budget/<int:year>/<int:month>/<str:week_or_iban>",
        views.overview,
        name="overview_detail",
    ),
    path(
        "budget/<int:year>/<int:month>/<str:week_or_iban>/",
        views.overview,
        name="overview_detail_slash",
    ),
]
