from django.urls import path

from budget import views

app_name = "budget"
urlpatterns = [
    path("", views.IndexView.as_view(), name="home"),
    path("<int:year>/<int:month>", views.IndexView.as_view(), name="home"),
    path("<int:year>/<int:month>/<int:week>", views.IndexView.as_view(), name="home"),
    path("<int:year>/<int:month>/<str:iban>", views.IndexView.as_view(), name="home"),
]
