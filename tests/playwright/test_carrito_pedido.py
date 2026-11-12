"""Flujo nuevo del cliente: elegir platos en el menú, seleccionar mesa, confirmar y seguir el pedido.

Este flujo no estaba cubierto por la suite de Selenium.
"""
import re

from playwright.sync_api import expect

import config
from pages import LoginPage, MenuPage, MisPedidosPage

CLIENTE = config.USUARIOS["cliente"]


def _entrar_como_cliente(page):
    LoginPage(page).iniciar_sesion(CLIENTE["email"], config.CLAVE_DEMO)
    expect(page.locator(".hero p.lead")).to_contain_text(CLIENTE["nombre"])


def test_carrito_vacio_muestra_mensaje(page, evidencia):
    _entrar_como_cliente(page)
    menu = MenuPage(page).abrir()
    expect(menu.carrito_vacio).to_be_visible()
    evidencia("carrito_vacio")


def test_confirmar_pedido_exige_seleccionar_mesa(page, evidencia):
    _entrar_como_cliente(page)
    menu = MenuPage(page).abrir().agregar("Gaseosa")
    expect(menu.confirmar).to_be_disabled()
    evidencia("sin_mesa_boton_deshabilitado")
    menu.seleccionar_primera_mesa()
    expect(menu.confirmar).to_be_enabled()
    evidencia("con_mesa_boton_habilitado")


def test_flujo_completo_del_pedido_del_cliente(page, evidencia):
    _entrar_como_cliente(page)
    menu = MenuPage(page).abrir()

    # 1. Armar el carrito: 2 pizzas pepperoni (2 x $32.000) y 1 gaseosa ($5.000) = $69.000
    menu.agregar("Pizza Pepperoni").sumar("Pizza Pepperoni").agregar("Gaseosa")
    expect(menu.total).to_contain_text("69.000")
    evidencia("carrito_armado")

    # 2. Elegir una mesa libre y confirmar
    menu.seleccionar_primera_mesa()
    expect(menu.confirmar).to_be_enabled()
    evidencia("mesa_seleccionada")
    menu.confirmar.click()

    # 3. El pedido aparece en «Mis pedidos», Pendiente y con el total correcto
    expect(page).to_have_url(re.compile(r"/mis-pedidos"))
    pedidos = MisPedidosPage(page)
    expect(pedidos.aviso_exito).to_contain_text("Pedido enviado")
    tarjeta = pedidos.primer_pedido
    expect(tarjeta).to_contain_text("Pendiente")
    expect(tarjeta).to_contain_text("69.000")
    expect(tarjeta).to_contain_text("2 × Pizza Pepperoni")
    expect(tarjeta).to_contain_text("1 × Gaseosa")
    evidencia("pedido_creado")

    # 4. Cancelar el pedido libera la mesa y devuelve el inventario
    pedidos.cancelar_primer_pedido()
    expect(pedidos.primer_pedido).to_contain_text("Cancelado")
    evidencia("pedido_cancelado")
