"""Fixtures: capturas de evidencia numeradas por paso. El navegador lo aporta pytest-playwright."""
import re

import pytest

import config


@pytest.fixture(scope="session")
def browser_context_args(browser_context_args):
    """Ventana de escritorio estándar y configuración regional de Colombia para todas las pruebas."""
    return {**browser_context_args, "viewport": {"width": 1366, "height": 850}, "locale": "es-CO"}


@pytest.fixture
def evidencia(page, request):
    config.EVIDENCIA.mkdir(parents=True, exist_ok=True)
    nombre = re.sub(r"[^A-Za-z0-9._-]+", "_", request.node.name)[:80]   # nombres válidos también en Windows/artefactos de CI
    contador = {"n": 0}

    def capturar(paso: str):
        contador["n"] += 1
        ruta = config.EVIDENCIA / f"{nombre}__{contador['n']:02d}_{paso}.png"
        page.screenshot(path=str(ruta))
        return ruta

    return capturar
