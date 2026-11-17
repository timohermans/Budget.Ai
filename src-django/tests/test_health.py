"""Health probe tests."""

from __future__ import annotations

from unittest import mock

from django.test import Client, SimpleTestCase, TestCase, override_settings


@override_settings(TEST_MODE=False)
class HealthCheckTests(SimpleTestCase):
    """The probes must stay anonymous and never depend on test-mode auth."""

    def test_live_returns_healthy_without_touching_database(self) -> None:
        with mock.patch("config.health.connection") as connection:
            response = Client().get("/health/live")

        self.assertEqual(200, response.status_code)
        self.assertEqual(b"Healthy", response.content)
        self.assertEqual("text/plain; charset=utf-8", response["Content-Type"])
        self.assertEqual("no-store, no-cache", response["Cache-Control"])
        connection.cursor.assert_not_called()

    def test_ready_returns_healthy_when_database_answers(self) -> None:
        with mock.patch("config.health.connection") as connection:
            response = Client().get("/health/ready")

        self.assertEqual(200, response.status_code)
        self.assertEqual(b"Healthy", response.content)
        self.assertEqual("text/plain; charset=utf-8", response["Content-Type"])
        connection.cursor.assert_called_once()

    def test_ready_returns_503_when_database_fails(self) -> None:
        with mock.patch("config.health.connection") as connection:
            connection.cursor.side_effect = RuntimeError("boom")
            with self.assertLogs("config.health", level="ERROR"):
                response = Client().get("/health/ready")

        self.assertEqual(503, response.status_code)
        self.assertEqual(b"Unhealthy", response.content)

    def test_ready_does_not_leak_exception_details(self) -> None:
        with mock.patch("config.health.connection") as connection:
            connection.cursor.side_effect = RuntimeError("secret")
            with self.assertLogs("config.health", level="ERROR"):
                response = Client().get("/health/ready")

        self.assertNotIn(b"secret", response.content)


@override_settings(TEST_MODE=False)
class HealthCheckDatabaseTests(TestCase):
    """The readiness probe executes a real query against the configured database."""

    def test_ready_returns_healthy_against_real_database(self) -> None:
        response = Client().get("/health/ready")

        self.assertEqual(200, response.status_code)
        self.assertEqual(b"Healthy", response.content)
