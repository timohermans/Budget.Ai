"""Merchant administration views, ported from the Merchants feature controllers."""

from __future__ import annotations

import logging
from urllib.parse import urlparse

from django.http import HttpRequest, HttpResponse
from django.shortcuts import render
from django.utils import timezone
from django.views.decorators.http import require_GET, require_POST

from apps.merchants import queries
from apps.merchants.forms import (
    MerchantClearForm,
    MerchantLinkForm,
    MerchantMapForm,
    MerchantOptionsForm,
    MerchantRowsForm,
)
from apps.merchants.models import Merchant, MerchantAlias, MerchantStatus
from apps.merchants.normalizer import normalize

logger = logging.getLogger(__name__)


def _valid_logo_url(value: str) -> str | None:
    candidate = value.strip()
    if not candidate:
        return None
    parsed = urlparse(candidate)
    if parsed.scheme.lower() not in {"http", "https"} or not parsed.netloc:
        return None
    return candidate


@require_GET
def index(request: HttpRequest) -> HttpResponse:
    rows = queries.get_rows("", "", "")
    recent_options = queries.get_options(None)
    partial = queries.RowsPartial(
        search="",
        sort="",
        dir="",
        rows=rows,
        recent_options=recent_options,
    )
    return render(
        request,
        "merchants/index.html",
        {"partial": partial, "rows": rows, "recent_options": recent_options},
    )


@require_GET
def rows(request: HttpRequest) -> HttpResponse:
    form = MerchantRowsForm(request.GET)
    data = form.cleaned_data if form.is_valid() else {}
    partial = queries.build_rows_partial(
        data.get("search", "") or "",
        data.get("sort", "") or "",
        data.get("dir", "") or "",
    )
    return render(request, "merchants/_rows.html", {"partial": partial})


@require_GET
def options(request: HttpRequest) -> HttpResponse:
    form = MerchantOptionsForm(request.GET)
    data = form.cleaned_data if form.is_valid() else {}
    name = data.get("name", "") or ""
    q = data.get("q", "") or ""
    options_list = queries.get_options(q)
    return render(
        request,
        "merchants/_options.html",
        {
            "name": name,
            "q": q,
            "search": data.get("search", "") or "",
            "sort": data.get("sort", "") or "",
            "dir": data.get("dir", "") or "",
            "options": options_list,
        },
    )


@require_POST
def map_merchant(request: HttpRequest) -> HttpResponse:
    form = MerchantMapForm(request.POST)
    data = form.cleaned_data if form.is_valid() else {}
    key = normalize(data.get("name", "") or "")
    logo_url = _valid_logo_url(data.get("logoUrl", "") or "")

    if key:
        if logo_url is not None:
            display_name = (data.get("displayName", "") or "").strip() or data.get("name", "")
            merchant = Merchant.objects.filter(name_normalized=key).first()
            if merchant is None:
                Merchant.objects.create(
                    name_normalized=key,
                    display_name=display_name,
                    logo_url=logo_url,
                    status=MerchantStatus.MAPPED,
                    updated_at=timezone.now(),
                )
                logger.info("Created merchant %s with display name %s", key, display_name)
            else:
                merchant.display_name = display_name
                merchant.logo_url = logo_url
                merchant.status = MerchantStatus.MAPPED
                merchant.updated_at = timezone.now()
                merchant.save(update_fields=["display_name", "logo_url", "status", "updated_at"])
                logger.info("Updated merchant %s", key)
        else:
            logger.warning(
                "Merchant map for %s rejected: logo URL %s is invalid",
                key,
                data.get("logoUrl", ""),
            )

    partial = queries.build_rows_partial(
        data.get("search", "") or "",
        data.get("sort", "") or "",
        data.get("dir", "") or "",
    )
    return render(request, "merchants/_rows.html", {"partial": partial})


@require_POST
def link_merchant(request: HttpRequest) -> HttpResponse:
    form = MerchantLinkForm(request.POST)
    data = form.cleaned_data if form.is_valid() else {}
    key = normalize(data.get("name", "") or "")
    target_key = normalize(data.get("merchantName", "") or "")

    if key and target_key and key != target_key:
        target = Merchant.objects.filter(name_normalized=target_key).first()
        already_canonical = Merchant.objects.filter(name_normalized=key).exists()
        if target is not None and not already_canonical:
            alias = MerchantAlias.objects.filter(name_normalized=key).first()
            if alias is None:
                MerchantAlias.objects.create(
                    name_normalized=key,
                    merchant=target,
                    created_at=timezone.now(),
                )
            else:
                alias.merchant = target
                alias.save(update_fields=["merchant"])
            logger.info(
                "Linked name %s to merchant %s",
                key,
                target.display_name or target_key,
            )

    partial = queries.build_rows_partial(
        data.get("search", "") or "",
        data.get("sort", "") or "",
        data.get("dir", "") or "",
    )
    return render(request, "merchants/_rows.html", {"partial": partial})


@require_POST
def clear_merchant(request: HttpRequest) -> HttpResponse:
    form = MerchantClearForm(request.POST)
    data = form.cleaned_data if form.is_valid() else {}
    key = normalize(data.get("name", "") or "")

    if key:
        merchant = Merchant.objects.filter(name_normalized=key).first()
        if merchant is not None:
            alias_count = MerchantAlias.objects.filter(merchant=merchant).count()
            merchant.delete()
            logger.info("Cleared merchant %s and its %s alias(es)", key, alias_count)
        else:
            alias = MerchantAlias.objects.filter(name_normalized=key).first()
            if alias is not None:
                alias.delete()
                logger.info("Cleared alias %s", key)

    partial = queries.build_rows_partial(
        data.get("search", "") or "",
        data.get("sort", "") or "",
        data.get("dir", "") or "",
    )
    return render(request, "merchants/_rows.html", {"partial": partial})
