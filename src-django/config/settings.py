"""Django settings for the Budget application.

The project is a behavioural port of the ASP.NET Core app in ``src/Budget.Web``.
Environment variables intentionally mirror the .NET configuration keys so both
implementations can share the same ``.env`` file.
"""

from __future__ import annotations

import os
from pathlib import Path
from typing import Any, cast

import dj_database_url
from dotenv import load_dotenv

BASE_DIR = Path(__file__).resolve().parent.parent
REPO_ROOT = BASE_DIR.parent


def _load_repository_env() -> None:
    """Load the first ``.env`` found walking up from this file."""
    for directory in (BASE_DIR, *BASE_DIR.parents):
        candidate = directory / ".env"
        if candidate.is_file():
            load_dotenv(candidate)
            return
    load_dotenv()


_load_repository_env()


def _env(name: str, default: str = "") -> str:
    return os.getenv(name, default).strip()


def _env_bool(name: str, default: bool = False) -> bool:
    raw = os.getenv(name)
    if raw is None or raw.strip() == "":
        return default
    return raw.strip().lower() in {"1", "true", "yes", "on"}


def _env_list(name: str, default: list[str] | None = None) -> list[str]:
    raw = os.getenv(name)
    if raw is None or raw.strip() == "":
        return list(default or [])
    return [item.strip() for item in raw.split(",") if item.strip()]


def _parse_npgsql_connection_string(connection: str) -> dict[str, Any]:
    """Translate an EF/Npgsql connection string into Django database settings."""
    values: dict[str, str] = {}
    for pair in connection.split(";"):
        if not pair.strip():
            continue
        key, _, value = pair.partition("=")
        values[key.strip().lower()] = value.strip()

    database: dict[str, Any] = {
        "ENGINE": "django.db.backends.postgresql",
        "NAME": values.get("database", "budget"),
        "USER": values.get("username", "budget"),
        "PASSWORD": values.get("password", "budget"),
        "HOST": values.get("host", "localhost"),
        "PORT": values.get("port", "5432"),
        "CONN_MAX_AGE": 60,
    }
    options: dict[str, str] = {}
    if values.get("sslmode"):
        options["sslmode"] = values["sslmode"]
    if options:
        database["OPTIONS"] = options
    return database


def _database_settings() -> dict[str, Any]:
    explicit_url = _env("DATABASE_URL")
    if explicit_url:
        parsed = dj_database_url.parse(explicit_url, conn_max_age=60)
        if parsed.get("ENGINE") == "django.db.backends.sqlite3":
            parsed.setdefault("OPTIONS", {})
            parsed["OPTIONS"].setdefault("timeout", 20)
        return cast(dict[str, Any], parsed)

    connection = _env("ConnectionStrings__Budget") or _env("BUDGET_DB_CONNECTION")
    if connection:
        if "://" in connection:
            return cast(dict[str, Any], dj_database_url.parse(connection, conn_max_age=60))
        return _parse_npgsql_connection_string(connection)

    return {
        "ENGINE": "django.db.backends.postgresql",
        "NAME": "budget",
        "USER": "budget",
        "PASSWORD": "budget",
        "HOST": "localhost",
        "PORT": "5432",
        "CONN_MAX_AGE": 60,
    }


SECRET_KEY = _env("BUDGET_SECRET_KEY") or _env("SECRET_KEY") or "django-insecure-dev-only-budget-ai"
DEBUG = _env_bool("BUDGET_DEBUG", default=_env("BUDGET_ENV", "development") != "production")
ALLOWED_HOSTS = _env_list("BUDGET_ALLOWED_HOSTS", ["*"] if DEBUG else [])
CSRF_TRUSTED_ORIGINS = _env_list("BUDGET_CSRF_TRUSTED_ORIGINS")

TEST_MODE = DEBUG and _env_bool("BUDGET_TEST_MODE", default=True)

DATABASES = {"default": _database_settings()}
DEFAULT_AUTO_FIELD = "django.db.models.AutoField"

INSTALLED_APPS = [
    "django.contrib.auth",
    "django.contrib.contenttypes",
    "django.contrib.sessions",
    "django.contrib.messages",
    "apps.budget",
    "apps.transactions",
    "apps.merchants",
]

MIDDLEWARE = [
    "django.middleware.security.SecurityMiddleware",
    "django.contrib.sessions.middleware.SessionMiddleware",
    "django.middleware.common.CommonMiddleware",
    "django.middleware.csrf.CsrfViewMiddleware",
    "django.contrib.auth.middleware.AuthenticationMiddleware",
    "config.middleware.TestModeAuthMiddleware",
    "django.contrib.messages.middleware.MessageMiddleware",
    "config.middleware.BudgetLoginRequiredMiddleware",
    "django.middleware.clickjacking.XFrameOptionsMiddleware",
]

ROOT_URLCONF = "config.urls"
WSGI_APPLICATION = "config.wsgi.application"
ASGI_APPLICATION = "config.asgi.application"

TEMPLATES = [
    {
        "BACKEND": "django.template.backends.django.DjangoTemplates",
        "DIRS": [BASE_DIR / "templates"],
        "APP_DIRS": True,
        "OPTIONS": {
            "context_processors": [
                "django.template.context_processors.request",
                "django.contrib.auth.context_processors.auth",
                "django.contrib.messages.context_processors.messages",
            ],
        },
    },
]

AUTHENTICATION_BACKENDS = ["django.contrib.auth.backends.ModelBackend"]

SESSION_ENGINE = "django.contrib.sessions.backends.signed_cookies"
SESSION_COOKIE_AGE = 8 * 60 * 60
SESSION_SAVE_EVERY_REQUEST = True
SESSION_COOKIE_HTTPONLY = True
SESSION_COOKIE_SAMESITE = "Lax"
SESSION_COOKIE_SECURE = _env_bool("BUDGET_SECURE_COOKIES", default=not DEBUG)
CSRF_COOKIE_SECURE = SESSION_COOKIE_SECURE

LOGIN_URL = "/oidc/authenticate/"
LOGIN_REDIRECT_URL = "/budget"
LOGOUT_REDIRECT_URL = "/"

LANGUAGE_CODE = "en-us"
TIME_ZONE = "UTC"
USE_I18N = False
USE_TZ = True

STATIC_URL = "/static/"
STATIC_ROOT = BASE_DIR / "staticfiles"
MEDIA_URL = "/media/"
MEDIA_ROOT = BASE_DIR / "media"

DATA_UPLOAD_MAX_MEMORY_SIZE = 30 * 1024 * 1024
FILE_UPLOAD_MAX_MEMORY_SIZE = 30 * 1024 * 1024

SECURE_PROXY_SSL_HEADER = ("HTTP_X_FORWARDED_PROTO", "https")
USE_X_FORWARDED_HOST = True
USE_X_FORWARDED_PORT = True

OIDC_AUTHORITY = _env("Oidc__Authority") or _env("OIDC_AUTHORITY")
OIDC_CLIENT_ID = _env("Oidc__ClientId") or _env("OIDC_CLIENT_ID")
OIDC_CLIENT_SECRET = _env("Oidc__ClientSecret") or _env("OIDC_CLIENT_SECRET")
OIDC_ENABLED = bool(OIDC_AUTHORITY)

if OIDC_ENABLED:
    authority = OIDC_AUTHORITY.rstrip("/")
    AUTHENTICATION_BACKENDS = ["config.auth.BudgetOIDCAuthenticationBackend"]
    OIDC_RP_CLIENT_ID = OIDC_CLIENT_ID
    OIDC_RP_CLIENT_SECRET = OIDC_CLIENT_SECRET
    OIDC_RP_SIGN_ALGO = "RS256"
    OIDC_RP_SCOPES = "openid profile email"
    OIDC_OP_AUTHORIZATION_ENDPOINT = f"{authority}/protocol/openid-connect/auth"
    OIDC_OP_TOKEN_ENDPOINT = f"{authority}/protocol/openid-connect/token"
    OIDC_OP_USER_ENDPOINT = f"{authority}/protocol/openid-connect/userinfo"
    OIDC_OP_JWKS_ENDPOINT = f"{authority}/protocol/openid-connect/certs"
    OIDC_VERIFY_SSL = True
    OIDC_STORE_ID_TOKEN = True

LOGGING = {
    "version": 1,
    "disable_existing_loggers": False,
    "formatters": {
        "default": {
            "format": "%(asctime)s %(levelname)s %(name)s %(message)s",
        },
    },
    "handlers": {
        "console": {
            "class": "logging.StreamHandler",
            "formatter": "default",
        },
    },
    "root": {
        "handlers": ["console"],
        "level": "INFO",
    },
    "loggers": {
        "django": {"handlers": ["console"], "level": "INFO", "propagate": False},
        "budget": {
            "handlers": ["console"],
            "level": "DEBUG" if DEBUG else "INFO",
            "propagate": False,
        },
        "transactions": {
            "handlers": ["console"],
            "level": "DEBUG" if DEBUG else "INFO",
            "propagate": False,
        },
        "merchants": {
            "handlers": ["console"],
            "level": "DEBUG" if DEBUG else "INFO",
            "propagate": False,
        },
    },
}

# Startup diagnostics mirror the .NET app's single startup log line.
_database = DATABASES["default"]
DATABASE_DESCRIPTION = (
    f"{_database.get('HOST', '')}/{_database.get('NAME', '')}"
    if _database.get("ENGINE") == "django.db.backends.postgresql"
    else str(_database.get("NAME", ""))
)
AUTH_MODE = "oidc" if OIDC_ENABLED else "cookie-only"
