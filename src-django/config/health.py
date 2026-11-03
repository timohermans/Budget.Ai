"""Health and root views."""

from __future__ import annotations

from django.db import connection
from django.http import HttpRequest, HttpResponse
from django.shortcuts import redirect


def root(request: HttpRequest) -> HttpResponse:
    return redirect("/budget")


def health_live(request: HttpRequest) -> HttpResponse:
    return HttpResponse("Healthy")


def health_ready(request: HttpRequest) -> HttpResponse:
    try:
        with connection.cursor() as cursor:
            cursor.execute("SELECT 1")
    except Exception:
        return HttpResponse("Unhealthy", status=503)
    return HttpResponse("Healthy")
