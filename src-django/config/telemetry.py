"""OpenTelemetry tracing and log export.

Configuration mirrors the .NET app: ``OpenTelemetry__Endpoint`` and
``OpenTelemetry__Headers`` come from the shared ``.env`` file, and the standard
``OTEL_*`` variables are honoured as fallbacks. Telemetry stays disabled while
no endpoint is configured.
"""

from __future__ import annotations

import atexit
from collections.abc import Mapping, Sequence
from typing import Any

from django.conf import settings
from opentelemetry import trace
from opentelemetry._logs import set_logger_provider
from opentelemetry.exporter.otlp.proto.http._log_exporter import OTLPLogExporter
from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter
from opentelemetry.instrumentation.django import DjangoInstrumentor
from opentelemetry.instrumentation.logging.handler import LoggingHandler
from opentelemetry.instrumentation.psycopg import PsycopgInstrumentor
from opentelemetry.sdk._logs import LoggerProvider
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.sdk.trace.sampling import ParentBased, TraceIdRatioBased
from opentelemetry.util.re import parse_env_headers

_configured = False


def _is_otel_value(value: object) -> bool:
    """Return whether ``value`` is representable as an OTel attribute value."""
    if value is None or isinstance(value, (bool, int, float, str, bytes)):
        return True
    if isinstance(value, Mapping):
        return all(isinstance(key, str) and _is_otel_value(item) for key, item in value.items())
    if isinstance(value, Sequence):
        return all(_is_otel_value(item) for item in value)
    return False


class BudgetLoggingHandler(LoggingHandler):
    """OTLP log handler that drops attributes OTel cannot represent.

    Django's ``django.server`` access logger passes the raw socket as
    ``extra={"request": ...}``. The base handler copies every non-standard
    record attribute into the OTel log record, so such values would make the
    SDK log a warning through this same handler and trip its recursion guard.
    """

    def _get_attributes(self, record: Any) -> tuple[dict[str, Any], str | None]:
        attributes, event_name = super()._get_attributes(record)
        cleaned = {key: value for key, value in attributes.items() if _is_otel_value(value)}
        return cleaned, event_name


def configure_telemetry() -> None:
    """Set up tracing and log export once per process.

    Must run after ``django.setup()`` and before Django builds its WSGI/ASGI
    handler: the Django instrumentation registers its middleware in
    ``settings.MIDDLEWARE``, which is only read when the handler is created.
    """
    global _configured
    if _configured or not settings.OTEL_ENABLED:
        return
    _configured = True

    resource = Resource.create(
        {
            "service.name": settings.OTEL_SERVICE_NAME,
            "deployment.environment.name": settings.OTEL_ENVIRONMENT,
        }
    )
    headers = (
        parse_env_headers(settings.OTEL_HEADERS, liberal=True) if settings.OTEL_HEADERS else None
    )

    if settings.OTEL_TRACES_ENABLED:
        _configure_tracing(resource, headers)
    if settings.OTEL_LOGS_ENABLED:
        _configure_logging(resource, headers)


def _configure_tracing(resource: Resource, headers: Mapping[str, str] | None) -> None:
    provider = TracerProvider(
        resource=resource,
        sampler=ParentBased(TraceIdRatioBased(settings.OTEL_TRACE_SAMPLE_RATIO)),
    )
    provider.add_span_processor(
        BatchSpanProcessor(
            OTLPSpanExporter(endpoint=settings.OTEL_TRACES_ENDPOINT, headers=headers)
        )
    )
    trace.set_tracer_provider(provider)
    atexit.register(provider.shutdown)

    DjangoInstrumentor().instrument(tracer_provider=provider)
    PsycopgInstrumentor().instrument(tracer_provider=provider)


def _configure_logging(resource: Resource, headers: Mapping[str, str] | None) -> None:
    provider = LoggerProvider(resource=resource)
    provider.add_log_record_processor(
        BatchLogRecordProcessor(
            OTLPLogExporter(endpoint=settings.OTEL_LOGS_ENDPOINT, headers=headers)
        )
    )
    set_logger_provider(provider)
    atexit.register(provider.shutdown)
