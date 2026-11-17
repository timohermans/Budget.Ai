"""Small presentation filters used by the ported Razor templates."""

from __future__ import annotations

from decimal import Decimal
from typing import Any

from django import template

register = template.Library()


@register.filter
def money(value: Any) -> str:
    """Render a numeric value with exactly two decimals and a decimal point."""
    if value is None or value == "":
        return ""
    if isinstance(value, Decimal):
        return format(value, ".2f")
    try:
        return format(Decimal(str(value)), ".2f")
    except Exception:
        return str(value)
