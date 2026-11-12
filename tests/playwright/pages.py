"""Page Objects de las pantallas probadas con Playwright."""
import re
import time

from playwright.sync_api import Page, expect

import config


def esperar_blazor(page: Page):
    """Blazor Server conecta su circuito tras cargar la página; un clic anterior se pierde."""
    page.wait_for_load_state("networkidle")
    page.wait_for_function("() => window.Blazor !== undefined")
    time.sleep(0.6)


class LoginPage:
    def __init__(self, page: Page):
        self.page = page

    def abrir(self):
        self.page.goto(f"{config.BASE_URL}/login")
        return self

    def iniciar_sesion(self, email: str, clave: str):
        self.abrir()
        self.page.locator("input[name=email]").fill(email)
        self.page.locator("input[name=password]").fill(clave)
        self.page.get_by_role("button", name="Entrar").click()
        return self

    @property
    def error(self):
        return self.page.locator(".alert-danger")


class HomePage:
    def __init__(self, page: Page):
        self.page = page

    @property
    def bienvenida(self):
        return self.page.locator(".hero p.lead")

    def opciones_menu(self) -> list[str]:
        enlaces = self.page.locator(".nav-scrollable .nav-link")
        enlaces.first.wait_for()
        return [t.strip() for t in enlaces.all_inner_texts() if t.strip()]


class MenuPage:
    """Menú digital con carrito de compra (rol Cliente)."""

    def __init__(self, page: Page):
        self.page = page

    def abrir(self):
        self.page.goto(f"{config.BASE_URL}/menu")
        esperar_blazor(self.page)
        return self

    def _tarjeta(self, plato: str):
        return self.page.locator(".plato-card", has_text=plato)

    def agregar(self, plato: str):
        """Hace clic en «Agregar» y reintenta si el circuito de Blazor aún no estaba conectado."""
        boton = self._tarjeta(plato).get_by_role("button", name="Agregar")
        for _ in range(4):
            boton.click()
            try:
                expect(self._tarjeta(plato).locator(".btn-group .btn.disabled")).to_have_text("1", timeout=2500)
                return self
            except AssertionError:
                continue
        raise AssertionError(f"No se pudo agregar «{plato}» al carrito")

    def sumar(self, plato: str):
        self._tarjeta(plato).locator(".btn-group .bi-plus").click()
        return self

    @property
    def total(self):
        return self.page.locator("aside .fs-5")

    @property
    def carrito_vacio(self):
        return self.page.get_by_text("Aún no has agregado platos")

    @property
    def selector_mesa(self):
        return self.page.locator("aside select")

    @property
    def confirmar(self):
        return self.page.get_by_role("button", name="Confirmar pedido")

    def seleccionar_primera_mesa(self):
        self.selector_mesa.select_option(index=1)
        return self


class MisPedidosPage:
    def __init__(self, page: Page):
        self.page = page

    @property
    def aviso_exito(self):
        return self.page.locator(".alert-success")

    @property
    def primer_pedido(self):
        return self.page.locator(".card", has_text="Pedido #").first

    def cancelar_primer_pedido(self):
        self.primer_pedido.get_by_role("button", name="Cancelar pedido").click()
        return self
