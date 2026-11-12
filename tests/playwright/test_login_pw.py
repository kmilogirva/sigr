"""Login y control de acceso con Playwright (equivalentes a F-01/F-04, F-02, S-02 y S-03 de Selenium)."""
import re

import pytest
from playwright.sync_api import expect

import config
from pages import HomePage, LoginPage

U = config.USUARIOS


@pytest.mark.humo
@pytest.mark.parametrize("rol, visibles, ocultas", [
    ("cliente", ["Mis pedidos", "Reservas"], ["Pedidos", "Administrar menú", "Caja", "Reportes", "Cocina", "Inventario"]),
    ("mesero", ["Pedidos", "Reservas"], ["Administrar menú", "Caja", "Reportes", "Cocina", "Inventario"]),
    ("admin", ["Pedidos", "Administrar menú", "Inventario", "Cupones", "Usuarios", "Cocina", "Caja", "Reportes"], ["Mis pedidos"]),
    ("cocinero", ["Cocina"], ["Pedidos", "Administrar menú", "Caja", "Reportes", "Inventario", "Mis pedidos"]),
])
def test_login_valido_muestra_menu_segun_rol(page, evidencia, rol, visibles, ocultas):
    LoginPage(page).iniciar_sesion(U[rol]["email"], config.CLAVE_DEMO)
    home = HomePage(page)
    expect(home.bienvenida).to_contain_text(U[rol]["nombre"])
    opciones = home.opciones_menu()
    evidencia("sesion_iniciada")
    for o in visibles:
        assert o in opciones, f"{rol} debería ver '{o}'"
    for o in ocultas:
        assert o not in opciones, f"{rol} NO debería ver '{o}'"


@pytest.mark.parametrize("email, clave", [
    (U["cliente"]["email"], "ClaveEquivocada1"),
    ("nadie@sigr.test", config.CLAVE_DEMO),
])
def test_login_con_credenciales_invalidas_muestra_error(page, evidencia, email, clave):
    login = LoginPage(page).iniciar_sesion(email, clave)
    expect(login.error).to_contain_text("Correo o contraseña incorrectos")
    evidencia("mensaje_de_error")


@pytest.mark.seguridad
@pytest.mark.parametrize("ruta", ["/reportes", "/cocina", "/admin/inventario", "/mis-pedidos"])
def test_rutas_protegidas_redirigen_al_login(page, evidencia, ruta):
    page.goto(f"{config.BASE_URL}{ruta}")
    expect(page).to_have_url(re.compile(r"/login\?ReturnUrl=", re.I))
    evidencia("redirigido_a_login")


@pytest.mark.seguridad
@pytest.mark.parametrize("ruta", ["/admin/inventario", "/admin/usuarios", "/caja"])
def test_cliente_no_accede_a_pantallas_de_administrador(page, evidencia, ruta):
    LoginPage(page).iniciar_sesion(U["cliente"]["email"], config.CLAVE_DEMO)
    expect(HomePage(page).bienvenida).to_be_visible()
    page.goto(f"{config.BASE_URL}{ruta}")
    expect(page.locator("body")).to_contain_text(re.compile("No tienes permiso|Acceso denegado|no tiene permiso", re.I))
    evidencia("acceso_denegado")
