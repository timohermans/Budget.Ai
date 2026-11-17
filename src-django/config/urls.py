"""Root URL configuration."""

from __future__ import annotations

from django.conf import settings
from django.urls import include, path

from config import health

urlpatterns = [
    path("", health.root),
    path("health/live", health.health_live),
    path("health/ready", health.health_ready),
    path("", include("apps.budget.urls")),
    path("", include("apps.transactions.urls")),
    path("", include("apps.merchants.urls")),
]

if settings.OIDC_ENABLED:
    urlpatterns.append(path("oidc/", include("mozilla_django_oidc.urls")))
