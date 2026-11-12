"""Fixtures: capturas de evidencia numeradas por paso. El navegador lo aporta pytest-playwright."""
import pytest

import config


@pytest.fixture(scope="session")
def browser_context_args(browser_context_args):
    """Ventana de escritorio estándar y configuración regional de Colombia para todas las pruebas."""
    return {**browser_context_args, "viewport": {"width": 1366, "height": 850}, "locale": "es-CO"}


@pytest.fixture
def evidencia(page, request):
    config.EVIDENCIA.mkdir(parents=True, exist_ok=True)
    nombre = request.node.name.replace("[", "_").replace("]", "").replace("/", "-")[:80]
    contador = {"n": 0}

    def capturar(paso: str):
        contador["n"] += 1
        ruta = config.EVIDENCIA / f"{nombre}__{contador['n']:02d}_{paso}.png"
        page.screenshot(path=str(ruta))
        return ruta

    return capturar
