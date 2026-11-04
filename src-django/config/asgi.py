"""ASGI entry point."""

from __future__ import annotations

import os

from django.core.asgi import get_asgi_application

os.environ.setdefault("DJANGO_SETTINGS_MODULE", "config.settings")

from config.telemetry import configure_telemetry  # noqa: E402

configure_telemetry()

application = get_asgi_application()
