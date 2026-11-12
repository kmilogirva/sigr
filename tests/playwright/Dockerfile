# Contenedor de pruebas E2E con Playwright (Python) sobre Chromium, en Linux
FROM mcr.microsoft.com/playwright/python:v1.49.0-noble

WORKDIR /tests/playwright
COPY requirements.txt .
RUN pip install --no-cache-dir --break-system-packages -r requirements.txt
COPY . .

ENV SIGR_BASE_URL=http://web:8080 \
    PYTHONDONTWRITEBYTECODE=1
ENTRYPOINT ["python", "-m", "pytest", "-p", "no:cacheprovider"]
