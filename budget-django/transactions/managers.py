from collections import defaultdict
import csv
import datetime
from decimal import Decimal
from io import TextIOWrapper
from typing import TypeVar, Optional, TYPE_CHECKING

from dateutil.relativedelta import relativedelta
from django.contrib.auth import get_user_model
from django.core.files.uploadedfile import UploadedFile
from django.db import models
from django.db.models.aggregates import Count

from core.exceptions import DomainError
from transactions.dataclasses import Summary, TransactionTemplateModel, WeekSummary, BalanceSummary

User = get_user_model()

Model = TypeVar("Model", bound=models.Model)

if TYPE_CHECKING:
    from .models import Transaction


class TransactionManager(models.Manager["Transaction"]):
    def get_queryset(self) -> models.QuerySet["Transaction", "Transaction"]:
        return super().get_queryset()

    def process_file(self, uploaded_file: UploadedFile, user: User) -> datetime.date:
        if uploaded_file.file is None:
            raise ValueError("Uploaded file is not available")
        csv_file = TextIOWrapper(uploaded_file.file, "latin-1")
        reader = csv.DictReader(csv_file)
        transactions = []
        date_max: datetime.date = datetime.date(datetime.MINYEAR, 1, 1)
        for row in reader:
            date = datetime.date.fromisoformat(row["Datum"])
            if date > date_max:
                date_max = date
            transaction = self.model(
                date=date,
                user=user,
                amount=Decimal(row["Bedrag"].replace(",", ".")),
                currency=row["Munt"],
                description=(
                    row["Omschrijving-1"]
                    + row["Omschrijving-2"]
                    + row["Omschrijving-3"]
                ).strip(),
                follow_number=row["Volgnr"],
                code=row["Code"],
                iban=row["IBAN/BBAN"],
                iban_other_party=row["Tegenrekening IBAN/BBAN"],
                name_other_party=row["Naam tegenpartij"],
            )

            transactions.append(transaction)

        self.bulk_create(transactions, ignore_conflicts=True)
        return date_max

    def get_summary_for(
        self, year: int, month: int, iban: Optional[str], user: User
    ):
        this_month = datetime.date(year, month, 1)
        this_month_end = this_month + relativedelta(months=1, days=-1)
        last_month = this_month + relativedelta(months=-1)
        next_month = this_month + relativedelta(months=1)

        ibans = [
            t["iban"]
            for t in self.filter(user=user)
            .values("iban")
            .annotate(amount_of_transactions=Count("id"))
            .order_by("-amount_of_transactions")
        ]
        transactions = self.filter(
            user=user, date__gte=last_month, date__lt=next_month
        ).order_by("-date", "-name_other_party").all()

        if len(ibans) == 0:
            return Summary(0, 0, 0, 0, 0)  # TODO: test no ibans yet

        iban = iban if iban is not None else ibans[0]

        if iban not in ibans:
            raise DomainError("Iban does not exist.")

        dates_per_week: defaultdict[int, list[datetime.date]] = defaultdict(list)
        summary_per_week: dict[int, WeekSummary] = {}
        for day in range(1, this_month_end.day + 1):
            date_in_month = datetime.date(this_month.year, this_month.month, day)
            week = date_in_month.isocalendar().week
            dates_per_week[week].append(date_in_month)

            if week not in summary_per_week:
                summary_per_week[week] = WeekSummary(week, 0, 0, 0)

        summary = Summary(0, 0, 0, 0, 0, summary_per_week)
        for t in transactions:
            transaction = TransactionTemplateModel(
                t.id, # type: ignore
                t.amount,
                t.date,
                t.is_fixed(ibans),
                t.is_not_fixed,
                t.name_other_party,
                t.description
            )
            date = transaction.date
            is_last_month = (
                date.year == last_month.year and date.month == last_month.month
            )
            is_this_month = not is_last_month
            amount = transaction.amount
            week = date.isocalendar().week
            is_target_iban = iban == t.iban

            if (
                is_target_iban
                and is_last_month
                and transaction.is_fixed
                and t.is_income()
            ):
                summary.income += amount
                summary.income_transactions.append(transaction)

            if (
                is_target_iban
                and is_last_month
                and transaction.is_fixed
                and t.is_expense()
            ):
                summary.expenses += abs(amount)
                summary.expense_transactions.append(transaction)

            if is_target_iban and is_this_month:
                week_summary = summary.weeks[week]
                if t.is_variable(ibans):
                    summary.spent += amount * -1
                    week_summary.spent += amount * -1

                week_summary.transactions.append(transaction)

            if is_this_month:
                if t.iban not in summary.iban_balances:
                    summary.iban_balances[t.iban] = BalanceSummary(
                        t.iban, 0
                    )
                balance = summary.iban_balances[t.iban]
                balance.balance += amount
                balance.transactions.append(transaction)

        summary.budget = abs(summary.income) - abs(summary.expenses)
        summary.left = summary.budget - summary.spent
        for week, week_summary in summary.weeks.items():
            budget_of_week = (summary.budget / this_month_end.day) * len(
                dates_per_week[week]
            )
            week_summary.budget = Decimal(budget_of_week)
            week_summary.left = abs(week_summary.budget) - abs(week_summary.spent)

        return summary
