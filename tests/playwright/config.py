"""Configuración central de las pruebas de Playwright (variables de entorno)."""
import os
from pathlib import Path

BASE_URL = os.getenv("SIGR_BASE_URL", "http://localhost:8080").rstrip("/")
CLAVE_DEMO = "Sigr2026!"
EVIDENCIA = Path(__file__).resolve().parent.parent / "evidencia" / "playwright" / "capturas"

USUARIOS = {
    "admin": {"email": "admin@sigr.test", "nombre": "Laura Martínez Ríos"},
    "mesero": {"email": "carlos.mesero@sigr.test", "nombre": "Carlos Gómez Peña"},
    "cliente": {"email": "maria.lopez@correo.test", "nombre": "María Fernanda López"},
    "cocinero": {"email": "cocina@sigr.test", "nombre": "Marta Salazar Ruiz"},
}
