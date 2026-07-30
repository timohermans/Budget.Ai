from django.contrib.auth import login
from django.contrib.auth.middleware import LoginRequiredMiddleware
from django.contrib.auth.models import User


class TestModeAuthMiddleware:
    def __init__(self, get_response):
        self.get_response = get_response

    def __call__(self, request):
        test_user_id = request.META.get("HTTP_X_TEST_USER")
        if test_user_id:
            user, _ = User.objects.get_or_create(
                username=test_user_id,
                defaults={"is_active": True},
            )
            request.user = user
            request.test_mode = True
            request._dont_enforce_csrf_checks = True
            login(request, user, backend="django.contrib.auth.backends.ModelBackend")
        return self.get_response(request)


class CustomLoginRequiredMiddleware(LoginRequiredMiddleware):
    """This middleware exists to exempt mozilla-django-oidc from middleware, because I use the the LoginRequiredMiddleware here :)"""
    def process_view(self, request, view_func, view_args, view_kwargs):
        if request.path.startswith("/oidc/"):
            return None
        return super().process_view(request, view_func, view_args, view_kwargs)