"""Fixtures compartidas: navegador, capturas de evidencia y limpieza de datos de prueba."""
import os
import re
import shutil
import subprocess
import time

import pytest
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service

import config


@pytest.fixture
def driver():
    config.EVIDENCIA.mkdir(parents=True, exist_ok=True)
    opts = Options()
    if config.HEADLESS:
        opts.add_argument("--headless=new")
    opts.add_argument("--ignore-certificate-errors")  # certificado de desarrollo autofirmado
    opts.add_argument("--window-size=1366,850")
    if os.getenv("SIGR_NO_SANDBOX") == "1":  # necesario al ejecutar Chrome como root dentro de un contenedor
        opts.add_argument("--no-sandbox")
        opts.add_argument("--disable-dev-shm-usage")
    if os.getenv("CHROME_BIN"):
        opts.binary_location = os.environ["CHROME_BIN"]
    service = Service(executable_path=os.environ["CHROMEDRIVER"]) if os.getenv("CHROMEDRIVER") else Service()
    d = webdriver.Chrome(options=opts, service=service)
    yield d
    d.quit()


@pytest.fixture
def evidencia(driver, request):
    """Devuelve una función que guarda una captura numerada del paso actual."""
    contador = {"n": 0}
    base = re.sub(r"[^A-Za-z0-9._-]+", "_", request.node.name)[:80]

    def capturar(paso: str):
        contador["n"] += 1
        time.sleep(0.4)
        ruta = config.EVIDENCIA / f"{base}__{contador['n']:02d}_{paso}.png"
        driver.save_screenshot(str(ruta))
        return ruta

    return capturar


@pytest.hookimpl(hookwrapper=True, tryfirst=True)
def pytest_runtest_makereport(item, call):
    outcome = yield
    rep = outcome.get_result()
    drv = item.funcargs.get("driver") if hasattr(item, "funcargs") else None
    if rep.when == "call" and rep.failed and drv is not None:
        nombre = re.sub(r"[^A-Za-z0-9._-]+", "_", item.name)[:80]
        drv.save_screenshot(str(config.EVIDENCIA / f"FALLO__{nombre}.png"))


@pytest.fixture(scope="session", autouse=True)
def limpiar_usuarios_de_prueba():
    """Los tests de registro crean usuarios con correo 'qa.*'; se eliminan al terminar la sesión."""
    yield
    if shutil.which("sqlcmd"):
        subprocess.run(
            ["sqlcmd", "-S", config.SQL_SERVER, "-E", "-C", "-Q",
             "DELETE FROM SIGR.dbo.Usuarios WHERE Email LIKE 'qa.%'"],
            capture_output=True, timeout=30)


def correo_unico(prefijo="registro") -> str:
    return f"qa.{prefijo}.{int(time.time() * 1000)}@sigr.test"
