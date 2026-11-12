"""Pruebas funcionales del formulario de inicio de sesión (/login) y del control de acceso por rol."""
import pytest

import config
from pages.home_page import HomePage
from pages.login_page import LoginPage

U = config.USUARIOS


@pytest.mark.humo
@pytest.mark.parametrize(
    "rol, visibles, ocultas",
    [
        # F-01 / F-04: login válido y opciones de menú según el rol
        ("cliente", ["Mis pedidos", "Reservas"], ["Pedidos", "Administrar menú", "Caja", "Reportes"]),
        ("mesero", ["Pedidos", "Reservas"], ["Administrar menú", "Caja", "Reportes"]),
        ("admin", ["Pedidos", "Administrar menú", "Caja", "Reportes"], ["Mis pedidos"]),
    ],
)
def test_f01_f04_login_valido_muestra_menu_segun_rol(driver, evidencia, rol, visibles, ocultas):
    usuario = U[rol]
    login = LoginPage(driver).abrir()
    login.escribir(usuario["email"], config.CLAVE_DEMO)
    evidencia("formulario_lleno")
    login.enviar()

    home = HomePage(driver)
    assert usuario["nombre"] in home.texto_bienvenida()
    opciones = home.opciones_menu()
    evidencia("sesion_iniciada")
    for opcion in visibles:
        assert opcion in opciones, f"{rol} debería ver '{opcion}'"
    for opcion in ocultas:
        assert opcion not in opciones, f"{rol} NO debería ver '{opcion}'"


@pytest.mark.parametrize(
    "caso, email, clave",
    [
        ("clave_incorrecta", U["cliente"]["email"], "ClaveEquivocada1"),
        ("correo_inexistente", "nadie@sigr.test", config.CLAVE_DEMO),
        ("clave_con_otra_capitalizacion", U["cliente"]["email"], "sigr2026!"),
    ],
)
def test_f02_login_con_credenciales_invalidas_muestra_error(driver, evidencia, caso, email, clave):
    login = LoginPage(driver).iniciar_sesion(email, clave)
    assert "Correo o contraseña incorrectos" in login.mensaje_error()
    assert "error=1" in driver.current_url
    evidencia("mensaje_de_error")


def test_f03_login_con_campos_vacios_es_bloqueado_por_el_navegador(driver, evidencia):
    login = LoginPage(driver).abrir()
    login.enviar()
    evidencia("envio_bloqueado")
    assert driver.current_url.endswith("/login"), "no debe haber navegación"
    assert login.validez(login.EMAIL)["valueMissing"] is True
    assert login.validez(login.PASSWORD)["valueMissing"] is True


@pytest.mark.seguridad
@pytest.mark.parametrize("intento", ["' OR 1=1 --", "admin@sigr.test' --", "\" OR \"\"=\""])
def test_s01_login_resiste_inyeccion_sql_en_el_servidor(driver, evidencia, intento):
    """Se desactiva la validación HTML5 para que el texto llegue al servidor."""
    login = LoginPage(driver).abrir()
    login.desactivar_validacion_html()
    login.escribir(intento, intento)
    login.enviar()
    assert "Correo o contraseña incorrectos" in login.mensaje_error()
    assert HomePage(driver).hay_elemento(HomePage.CERRAR_SESION) is False, "no debe quedar autenticado"
    evidencia("sin_acceso")


@pytest.mark.seguridad
@pytest.mark.parametrize("ruta", ["/reportes", "/caja", "/pedidos", "/reservas", "/mis-pedidos"])
def test_s02_rutas_protegidas_redirigen_al_login_sin_sesion(driver, evidencia, ruta):
    driver.get(config.BASE_URL + ruta)
    LoginPage(driver).esperar_url_contiene("/login")
    evidencia("redirigido_a_login")
    # La app conserva la ruta pedida para volver tras el login (el nombre del parámetro es ReturnUrl).
    assert f"returnurl={ruta.replace('/', '%2f')}" in driver.current_url.lower()


@pytest.mark.seguridad
def test_s03_cliente_no_puede_abrir_pantallas_de_administrador(driver, evidencia):
    LoginPage(driver).iniciar_sesion(U["cliente"]["email"], config.CLAVE_DEMO)
    HomePage(driver).texto_bienvenida()
    driver.get(config.BASE_URL + "/reportes")
    evidencia("acceso_denegado")
    cuerpo = driver.find_element("tag name", "body").text
    assert "Reportes de ventas" not in cuerpo
    assert "No tienes permiso" in cuerpo or "acceso-denegado" in driver.current_url


def test_f08_cerrar_sesion_termina_la_sesion(driver, evidencia):
    LoginPage(driver).iniciar_sesion(U["cliente"]["email"], config.CLAVE_DEMO)
    home = HomePage(driver)
    home.texto_bienvenida()
    home.cerrar_sesion()
    LoginPage(driver).esperar_url_contiene("/login")
    evidencia("sesion_cerrada")
    driver.get(config.BASE_URL + "/reservas")
    LoginPage(driver).esperar_url_contiene("/login")
