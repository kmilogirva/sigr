"""Pruebas del flujo de pedidos del mesero que dependen de reglas del servidor."""
import config
from pages.home_page import HomePage
from pages.login_page import LoginPage
from pages.pedidos_page import PedidoDetallePage, PedidosPage

import pytest


@pytest.mark.seguridad
def test_s06_un_pedido_vacio_no_puede_avanzar(driver, evidencia):
    """H-03: un pedido sin ítems no debe poder salir de «Pendiente»."""
    LoginPage(driver).iniciar_sesion(config.USUARIOS["mesero"]["email"], config.CLAVE_DEMO)
    HomePage(driver).texto_bienvenida()

    PedidosPage(driver).abrir().crear_pedido_en_primera_mesa_libre()
    detalle = PedidoDetallePage(driver)
    try:
        evidencia("pedido_vacio_creado")
        detalle.avanzar()
        assert "no tiene ítems" in detalle.mensaje_error(), "HALLAZGO H-03: el pedido vacío avanzó"
        evidencia("avance_rechazado")
        assert detalle.estado() == "Pendiente"
    finally:
        detalle.cancelar()   # libera la mesa para no dejar datos de prueba
