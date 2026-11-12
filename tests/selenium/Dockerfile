# Contenedor de pruebas funcionales de interfaz: Python + pytest + Selenium + Chromium (Linux)
FROM python:3.12-slim

RUN apt-get update \
 && apt-get install -y --no-install-recommends chromium chromium-driver fonts-liberation \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /tests/selenium
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY . .

# Chrome corre como root dentro del contenedor: necesita --no-sandbox (lo activa SIGR_NO_SANDBOX)
ENV CHROME_BIN=/usr/bin/chromium \
    CHROMEDRIVER=/usr/bin/chromedriver \
    SIGR_NO_SANDBOX=1 \
    SIGR_HEADLESS=1 \
    PYTHONDONTWRITEBYTECODE=1

ENTRYPOINT ["python", "-m", "pytest", "-p", "no:cacheprovider"]
