"""Configuración central de las pruebas (se puede sobreescribir con variables de entorno)."""
import os
from pathlib import Path

BASE_URL = os.getenv("SIGR_BASE_URL", "https://localhost:7072").rstrip("/")
HEADLESS = os.getenv("SIGR_HEADLESS", "1") == "1"
TIMEOUT = int(os.getenv("SIGR_TIMEOUT", "10"))
SQL_SERVER = os.getenv("SIGR_SQL_SERVER", r"localhost\SQLEXPRESS")
EVIDENCIA = Path(__file__).resolve().parent.parent / "evidencia" / "selenium"

CLAVE_DEMO = "Sigr2026!"

# Datos de prueba separados del código de los tests (usuarios ficticios del seed).
USUARIOS = {
    "admin": {"email": "admin@sigr.test", "nombre": "Laura Martínez Ríos", "rol": "Administrador"},
    "mesero": {"email": "carlos.mesero@sigr.test", "nombre": "Carlos Gómez Peña", "rol": "Mesero"},
    "cliente": {"email": "maria.lopez@correo.test", "nombre": "María Fernanda López", "rol": "Cliente"},
}
