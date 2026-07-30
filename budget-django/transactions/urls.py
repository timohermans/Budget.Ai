from django.urls import path
from . import views

app_name="transactions"
urlpatterns = [
    path('toggle-fixed', views.toggle_fixed, name='toggle-fixed'),
    path('upload', views.TransactionUploadView.as_view(), name='upload'),
]