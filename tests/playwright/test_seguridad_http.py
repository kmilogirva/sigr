"""Cabeceras de seguridad, política CSP y ruta de retorno segura (hallazgos H-05, H-06 y H-07)."""
import pytest
from playwright.sync_api import expect

import config

CABECERAS_ESPERADAS = {
    "x-content-type-options": "nosniff",
    "x-frame-options": "DENY",
    "referrer-policy": "strict-origin-when-cross-origin",
    "cross-origin-opener-policy": "same-origin",
    "cross-origin-resource-policy": "same-origin",
}


@pytest.mark.seguridad
def test_las_respuestas_incluyen_las_cabeceras_de_seguridad(page, evidencia):
    respuesta = page.goto(f"{config.BASE_URL}/login")
    cabeceras = {k.lower(): v for k, v in respuesta.headers.items()}
    for nombre, valor in CABECERAS_ESPERADAS.items():
        assert cabeceras.get(nombre) == valor, f"falta o es distinta la cabecera {nombre}"
    assert "permissions-policy" in cabeceras
    csp = cabeceras["content-security-policy"]
    assert "frame-ancestors 'none'" in csp and "object-src 'none'" in csp and "*" not in csp
    evidencia("pagina_login")


@pytest.mark.seguridad
def test_la_politica_csp_no_rompe_la_aplicacion(page, evidencia):
    """Con la CSP activa, la página sigue funcionando: sin violaciones y con el circuito de Blazor conectado."""
    violaciones = []
    page.on("console", lambda m: violaciones.append(m.text) if "Content Security Policy" in m.text else None)
    page.goto(f"{config.BASE_URL}/menu")
    page.wait_for_load_state("networkidle")
    page.wait_for_function("() => window.Blazor !== undefined")
    expect(page.locator(".plato-card").first).to_be_visible()
    evidencia("menu_con_csp")
    assert violaciones == [], f"violaciones de CSP: {violaciones}"


@pytest.mark.seguridad
def test_los_iconos_se_cargan_desde_el_propio_sitio(page):
    """H-06: ya no hay recursos de una CDN externa sin atributo de integridad."""
    externos = []
    page.on("request", lambda r: externos.append(r.url) if ("cdn." in r.url or "jsdelivr" in r.url) else None)
    page.goto(f"{config.BASE_URL}/login")
    page.wait_for_load_state("networkidle")
    assert externos == []
    expect(page.locator("link[href*='bootstrap-icons']")).to_have_count(1)


@pytest.mark.seguridad
@pytest.mark.parametrize("peligrosa", ["//sitio-malicioso.com", "http://sitio-malicioso.com", "javascript:alert(1)"])
def test_returnurl_peligrosa_no_se_refleja_en_el_formulario(page, evidencia, peligrosa):
    """H-07: el valor del parámetro ReturnUrl solo se refleja si es una ruta local segura."""
    page.goto(f"{config.BASE_URL}/login?ReturnUrl={peligrosa}")
    assert page.locator("input[name=returnUrl]").get_attribute("value") == ""
    evidencia("sin_reflejo")


@pytest.mark.seguridad
def test_returnurl_local_se_conserva(page):
    page.goto(f"{config.BASE_URL}/login?ReturnUrl=%2Freportes")
    assert page.locator("input[name=returnUrl]").get_attribute("value") == "/reportes"
