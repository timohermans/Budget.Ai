"""Authentication helpers for OIDC and development test mode."""

from __future__ import annotations

from typing import Any

from django.contrib.auth import get_user_model
from django.http import HttpRequest
from mozilla_django_oidc.auth import OIDCAuthenticationBackend

UserModel = get_user_model()


class BudgetOIDCAuthenticationBackend(OIDCAuthenticationBackend):  # type: ignore[misc]
    """Use the OIDC ``sub`` claim as the Django username and transaction user id."""

    def get_username(self, claims: dict[str, Any]) -> str:
        return str(claims.get("sub", ""))

    def filter_users_by_claims(self, claims: dict[str, Any]) -> Any:
        subject = claims.get("sub")
        if not subject:
            return UserModel.objects.none()
        return UserModel.objects.filter(username=subject)

    def create_user(self, claims: dict[str, Any]) -> Any:
        subject = str(claims.get("sub", ""))
        user = UserModel.objects.create_user(
            username=subject,
            email=str(claims.get("email", "")),
        )
        user.first_name = str(claims.get("given_name", ""))[:150]
        user.last_name = str(claims.get("family_name", ""))[:150]
        user.save(update_fields=["first_name", "last_name"])
        return user

    def update_user(self, user: Any, claims: dict[str, Any]) -> Any:
        user.email = str(claims.get("email", user.email))
        user.first_name = str(claims.get("given_name", user.first_name))[:150]
        user.last_name = str(claims.get("family_name", user.last_name))[:150]
        user.save(update_fields=["email", "first_name", "last_name"])
        return user


class TestUser:
    """A database-free authenticated user used by the development test mode.

    The .NET test mode deliberately does not create or look up a user record, so
    this object only exposes the identity surface the rest of the app needs.
    """

    is_authenticated = True
    is_anonymous = False
    is_active = True
    is_staff = False
    is_superuser = False

    def __init__(self, user_id: str) -> None:
        self.user_id = user_id
        self.pk = user_id
        self.id = user_id

    def get_username(self) -> str:
        return self.user_id

    def __str__(self) -> str:
        return self.user_id

    def has_perm(self, perm: str, obj: object | None = None) -> bool:
        return False

    def has_module_perms(self, app_label: str) -> bool:
        return False


def get_user_id(request: HttpRequest) -> str:
    """Return the authenticated transaction user id.

    OIDC users use their Django username (the provider ``sub``); test-mode users
    use the header value stored in the session.
    """
    user = request.user
    if user.is_authenticated:
        return user.get_username()
    return ""
