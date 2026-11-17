"""Project middleware for test-mode auth and htmx-aware login protection."""

from __future__ import annotations

from collections.abc import Callable
from typing import Any, cast

from django.conf import settings
from django.contrib.auth.views import redirect_to_login
from django.http import HttpRequest, HttpResponse, HttpResponseBase

from config.auth import TestUser

TEST_USER_SESSION_KEY = "budget_test_user_id"

PUBLIC_PREFIXES = (
    "/health/",
    "/oidc/",
    "/static/",
    "/media/",
    "/favicon.ico",
)


class TestModeAuthMiddleware:
    """Authenticate ``X-Test-User`` requests without touching the database.

    Only active when ``settings.TEST_MODE`` is true (development by default).
    The user id is persisted in the signed-cookie session, matching the test-mode
    contract, and antiforgery validation is bypassed for those requests.
    """

    def __init__(self, get_response: Callable[[HttpRequest], HttpResponseBase]) -> None:
        self.get_response = get_response

    def __call__(self, request: HttpRequest) -> HttpResponseBase:
        # Django's HttpRequest is intentionally extensible; annotate the two
        # dynamic attributes this project adds instead of subclassing it.
        augmented = cast(Any, request)

        if not settings.TEST_MODE:
            augmented.test_mode = False
            return self.get_response(request)

        header_user = request.headers.get("X-Test-User")
        session_user = request.session.get(TEST_USER_SESSION_KEY)
        test_user_id = header_user or session_user

        if test_user_id:
            if header_user and session_user != header_user:
                request.session[TEST_USER_SESSION_KEY] = header_user
            augmented.user = TestUser(str(test_user_id))
            augmented.test_mode = True
            augmented._dont_enforce_csrf_checks = True
        else:
            augmented.test_mode = False

        return self.get_response(request)


class BudgetLoginRequiredMiddleware:
    """Require authentication for every non-public request.

    Unlike Django's stock middleware this returns a bare 401 for htmx requests,
    so the client can reload the page instead of swapping a login page into a
    fragment.
    """

    def __init__(self, get_response: Callable[[HttpRequest], HttpResponseBase]) -> None:
        self.get_response = get_response

    def __call__(self, request: HttpRequest) -> HttpResponseBase:
        path = request.path
        is_public = path.startswith(PUBLIC_PREFIXES)
        if is_public or request.user.is_authenticated:
            return self.get_response(request)

        if request.headers.get("HX-Request"):
            return HttpResponse(status=401)

        return redirect_to_login(request.get_full_path())
