import datetime
from typing import Any, cast

from dateutil.relativedelta import relativedelta
from django.views.generic import TemplateView

from transactions.models import Transaction
from user.type import User


class IndexView(TemplateView):
    template_name = "budget/index.html"

    def get_context_data(self, **kwargs: Any) -> dict[str, Any]:
        context = super().get_context_data(**kwargs)
        if "year" not in context:
            context["year"] = datetime.date.today().year
            context["month"] = datetime.date.today().month

        date = datetime.date(context["year"], context["month"], 1)
        context["date"] = date
        context["date_previous"] = date + relativedelta(months=-1)
        context["date_next"] = date + relativedelta(months=1)

        context["month_display"] = date.strftime("%B")
        context["month_start"] = date.strftime("%d %b")
        context["month_end"] = (date + relativedelta(months=1, days=-1)).strftime("%d %b") 

        user = cast(User, self.request.user)

        # TODO: Figure out how caching would work, as this endpoints get hit pretty often
        context["summary"] = Transaction.objects.get_summary_for(
            date.year, date.month, iban=None, user=user
        )
        if "week" not in context:
            context["week"] = 0

        return context