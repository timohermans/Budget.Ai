"""Counterparty name normalization, ported from MerchantNameNormalizer.cs.

The same function is used for transaction names, canonical merchant keys, and
alias keys, so the values can never drift apart.
"""

from __future__ import annotations

import re

WHITESPACE_RUN = re.compile(r"\s+")
HYPHEN_SPACING = re.compile(r"\s*-\s*")


def normalize(name: str | None) -> str:
    if name is None or not name.strip():
        return ""

    normalized = name.strip().lower()
    normalized = WHITESPACE_RUN.sub(" ", normalized)
    normalized = HYPHEN_SPACING.sub(" - ", normalized)
    normalized = WHITESPACE_RUN.sub(" ", normalized)
    return normalized.strip()
