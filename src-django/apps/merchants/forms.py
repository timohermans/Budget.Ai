"""Forms for the merchant admin endpoints."""

from __future__ import annotations

from django import forms


class MerchantRowsForm(forms.Form):
    search = forms.CharField(required=False)
    sort = forms.CharField(required=False)
    dir = forms.CharField(required=False)


class MerchantOptionsForm(forms.Form):
    name = forms.CharField(required=False)
    q = forms.CharField(required=False)
    search = forms.CharField(required=False)
    sort = forms.CharField(required=False)
    dir = forms.CharField(required=False)


class MerchantMapForm(forms.Form):
    name = forms.CharField(required=False)
    displayName = forms.CharField(required=False)
    logoUrl = forms.CharField(required=False)
    search = forms.CharField(required=False)
    sort = forms.CharField(required=False)
    dir = forms.CharField(required=False)


class MerchantLinkForm(forms.Form):
    name = forms.CharField(required=False)
    merchantName = forms.CharField(required=False)
    search = forms.CharField(required=False)
    sort = forms.CharField(required=False)
    dir = forms.CharField(required=False)


class MerchantClearForm(forms.Form):
    name = forms.CharField(required=False)
    search = forms.CharField(required=False)
    sort = forms.CharField(required=False)
    dir = forms.CharField(required=False)
