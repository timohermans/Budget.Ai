FROM python:3.14 AS builder

RUN mkdir /app

WORKDIR /app

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1 
ENV PIP_DEFAULT_TIMEOUT=100
ENV POETRY_VERSION=2.3.4
ENV POETRY_HOME="/opt/poetry"
ENV VIRTUAL_ENV="/opt/pysetup/venv"

# Create the virtual env
RUN python -m venv $VIRTUAL_ENV
# Activate the virtual env
ENV PATH="$VIRTUAL_ENV/bin:$PATH" 


RUN pip install --upgrade pip 
RUN pip install --no-cache-dir poetry==2.3.4
ENV PATH="$POETRY_HOME/bin:$PATH"

COPY pyproject.toml poetry.lock ./

RUN poetry install --no-root --no-interaction --no-ansi

COPY . .

# RUN python manage.py collectstatic --noinput

# Final stage (small image)
FROM python:3.14-slim

ENV POETRY_HOME="/opt/poetry"
ENV VIRTUAL_ENV="/opt/pysetup/venv"
ENV PATH="$VIRTUAL_ENV/bin:$PATH" 

RUN apt-get update && apt-get install -y --no-install-recommends \
    libpq-dev curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy installed packages from builder
COPY --from=builder $VIRTUAL_ENV $VIRTUAL_ENV
# Copy application code
COPY --from=builder /app /app

# Optional: create non‑root user
RUN useradd --create-home django && chown -R django:django /app
USER django

EXPOSE 8000

# Use gunicorn (WSGI) – see explanation below
CMD ["gunicorn", "--bind", "0.0.0.0:8000", "config.wsgi"]