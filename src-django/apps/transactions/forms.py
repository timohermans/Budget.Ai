"""Forms for transaction endpoints."""

from __future__ import annotations

from django import forms


class UploadForm(forms.Form):
    file = forms.FileField(required=False)


class ToggleFixedForm(forms.Form):
    id = forms.IntegerField(min_value=1)
