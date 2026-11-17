"""Health and root views.

Behavioural port of the ASP.NET Core endpoints in ``src/Budget.Web/Program.cs``:

* ``/health/live`` runs no checks at all (``Predicate = _ => false``) and never
  touches the database, so a liveness probe cannot fail because of the database.
* ``/health/ready`` verifies the database (``AddDbContextCheck``), which ``SELECT 1``
  stands in for here.
* Both are anonymous and answer with ``text/plain`` "Healthy"/"Unhealthy" plus
  the same no-cache headers ``MapHealthChecks`` emits.
"""

from __future__ import annotations

import logging

from django.db import connection
from django.http import HttpRequest, HttpResponse
from django.shortcuts import redirect

logger = logging.getLogger(__name__)

# Emitted by ASP.NET Core's health check middleware so proxies never cache a probe.
_NO_CACHE_HEADERS = {
    "Cache-Control": "no-store, no-cache",
    "Pragma": "no-cache",
    "Expires": "Thu, 01 Jan 1970 00:00:00 GMT",
}


def _health_response(body: str, status: int = 200) -> HttpResponse:
    response = HttpResponse(body, status=status, content_type="text/plain; charset=utf-8")
    for header, value in _NO_CACHE_HEADERS.items():
        response[header] = value
    return response


def root(request: HttpRequest) -> HttpResponse:
    return redirect("/budget")


def health_live(request: HttpRequest) -> HttpResponse:
    return _health_response("Healthy")


def health_ready(request: HttpRequest) -> HttpResponse:
    try:
        with connection.cursor() as cursor:
            cursor.execute("SELECT 1")
    except Exception:
        logger.exception("Readiness check failed: database is not reachable")
        return _health_response("Unhealthy", status=503)
    return _health_response("Healthy")
