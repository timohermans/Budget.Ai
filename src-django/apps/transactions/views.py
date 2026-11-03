"""Transaction endpoints: CSV upload and fixed toggle."""

from __future__ import annotations

import logging

from django.http import HttpRequest, HttpResponse, HttpResponseNotFound, HttpResponseRedirect
from django.shortcuts import render
from django.views.decorators.http import require_POST

from apps.budget import queries, services
from apps.budget.services import TransactionOverview, TransactionTemplate
from apps.transactions import importer
from apps.transactions.classification import is_fixed
from apps.transactions.forms import ToggleFixedForm, UploadForm
from apps.transactions.models import Transaction
from config.auth import get_user_id

logger = logging.getLogger(__name__)


@require_POST
def upload(request: HttpRequest) -> HttpResponse:
    """Import the uploaded Rabobank CSV and redirect to its most recent month."""
    user_id = get_user_id(request)
    form = UploadForm(request.POST, request.FILES)
    uploaded = request.FILES.get("file")

    if not form.is_valid() or uploaded is None:
        logger.warning("CSV upload for user %s rejected: no file provided", user_id)
        return render(request, "transactions/error.html", status=200)

    try:
        content = uploaded.read()
        max_date = importer.process(content, user_id)
    except Exception:
        logger.exception(
            "Rabobank CSV import failed for user %s (file: %s)", user_id, uploaded.name
        )
        return render(request, "transactions/error.html", status=200)

    if max_date is None:
        logger.info(
            "Imported Rabobank CSV %s (%s bytes) for user %s; no records; redirecting to /budget",
            uploaded.name,
            uploaded.size,
            user_id,
        )
        return HttpResponseRedirect("/budget")
    else:
        logger.info(
            "Imported Rabobank CSV %s (%s bytes) for user %s; redirecting to %s-%02d",
            uploaded.name,
            uploaded.size,
            user_id,
            max_date.year,
            max_date.month,
        )

    return HttpResponseRedirect(f"/budget/{max_date.year}/{max_date.month}")


@require_POST
def toggle_fixed(request: HttpRequest) -> HttpResponse:
    """Flip a transaction's fixed flag and return the htmx fragment with OOB totals."""
    user_id = get_user_id(request)
    form = ToggleFixedForm(request.POST)
    if not form.is_valid():
        return HttpResponseNotFound()

    transaction_id = form.cleaned_data["id"]
    transaction = Transaction.objects.filter(id=transaction_id, user_id=user_id).first()
    if transaction is None:
        logger.warning(
            "Toggle-fixed for transaction %s rejected: not found for user %s",
            transaction_id,
            user_id,
        )
        return HttpResponseNotFound()

    transaction.is_not_fixed = not transaction.is_not_fixed
    transaction.save(update_fields=["is_not_fixed"])
    logger.info(
        "Toggled transaction %s for user %s: IsNotFixed=%s",
        transaction.id,
        user_id,
        transaction.is_not_fixed,
    )

    own_ibans = queries.get_own_ibans(user_id)
    first_of_month = transaction.date.replace(day=1)
    last_month = (
        first_of_month.replace(year=first_of_month.year - 1, month=12)
        if first_of_month.month == 1
        else first_of_month.replace(month=first_of_month.month - 1)
    )
    if first_of_month.month == 12:
        next_month = first_of_month.replace(year=first_of_month.year + 1, month=1)
    else:
        next_month = first_of_month.replace(month=first_of_month.month + 1)

    window = [
        TransactionOverview(transaction=item, display_name=None, logo_url=None)
        for item in Transaction.objects.filter(
            user_id=user_id,
            date__gte=last_month,
            date__lt=next_month,
        ).order_by("-date", "-name_other_party")
    ]
    summary = services.calculate(
        transaction.date.year,
        transaction.date.month,
        None,
        window,
        own_ibans,
    )
    week_number = transaction.date.isocalendar().week
    week_summary = summary.weeks[week_number]

    template = TransactionTemplate(
        id=transaction.id,
        amount=transaction.amount,
        date=transaction.date,
        is_fixed=is_fixed(transaction, own_ibans),
        is_not_fixed=transaction.is_not_fixed,
        name_other_party=transaction.name_other_party,
        description=transaction.description,
    )
    context = {
        "transaction": template,
        "week_number": week_number,
        "spent": summary.spent,
        "left": summary.left,
        "week_spent": week_summary.spent,
        "week_left": week_summary.left,
        "week_budget": week_summary.budget,
        "show_oob": True,
    }
    return render(request, "transactions/_toggle_fixed.html", context)
