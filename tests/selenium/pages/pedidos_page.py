import re
import time

from selenium.common.exceptions import StaleElementReferenceException
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import Select, WebDriverWait

import config

from .base_page import BasePage


class PedidosPage(BasePage):
    """Lista de pedidos en curso (rol Mesero o Administrador)."""

    ruta = "/pedidos"
    MESA = (By.CSS_SELECTOR, "select.form-select")
    CREAR = (By.XPATH, "//button[contains(., 'Crear pedido')]")

    def crear_pedido_en_primera_mesa_libre(self) -> int:
        # Blazor Server tarda un instante en conectar su circuito: un cambio hecho antes se pierde.
        # Se repite la selección (0 -> 1, para forzar el evento) hasta que el botón se habilite.
        espera = WebDriverWait(self.driver, 20, poll_frequency=0.5, ignored_exceptions=[StaleElementReferenceException])

        def seleccionar_y_comprobar(d):
            lista = Select(d.find_element(*self.MESA))
            if len(lista.options) < 2:
                return False
            lista.select_by_index(0)
            lista.select_by_index(1)
            time.sleep(0.8)
            return d.find_element(*self.CREAR).is_enabled()

        espera.until(seleccionar_y_comprobar)
        espera.until(lambda d: d.find_element(*self.CREAR).click() or True)
        self.wait.until(lambda d: re.search(r"/pedidos/\d+$", d.current_url))
        return int(self.driver.current_url.rsplit("/", 1)[1])


class PedidoDetallePage(BasePage):
    """Detalle de un pedido: ítems, avance de estado y cancelación."""

    AVANZAR = (By.XPATH, "//button[contains(., 'Avanzar estado')]")
    CANCELAR = (By.XPATH, "//button[contains(., 'Cancelar pedido')]")
    ALERTA_ERROR = (By.CSS_SELECTOR, ".alert-danger")
    ESTADO = (By.CSS_SELECTOR, "article p strong")

    def avanzar(self):
        self.esperar_visible(self.AVANZAR).click()
        return self

    def cancelar(self):
        self.esperar_visible(self.CANCELAR).click()
        return self

    def mensaje_error(self) -> str:
        return self.esperar_visible(self.ALERTA_ERROR).text

    def estado(self) -> str:
        return self.esperar_visible(self.ESTADO).text
