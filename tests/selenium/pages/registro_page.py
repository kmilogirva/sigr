from selenium.webdriver.common.by import By

from .base_page import BasePage


class RegistroPage(BasePage):
    ruta = "/registro"
    NOMBRE = (By.NAME, "nombre")
    EMAIL = (By.NAME, "email")
    TELEFONO = (By.NAME, "telefono")
    PASSWORD = (By.NAME, "password")
    ENVIAR = (By.CSS_SELECTOR, "form button")
    ALERTA_ERROR = (By.CSS_SELECTOR, ".alert-danger")

    def llenar(self, nombre="", email="", telefono="", password=""):
        self.esperar_visible(self.NOMBRE)
        for loc, valor in ((self.NOMBRE, nombre), (self.EMAIL, email), (self.TELEFONO, telefono), (self.PASSWORD, password)):
            campo = self.driver.find_element(*loc)
            campo.clear()
            campo.send_keys(valor)
        return self

    def enviar(self):
        self.driver.find_element(*self.ENVIAR).click()
        return self

    def registrar(self, **datos):
        return self.abrir().llenar(**datos).enviar()

    def mensaje_error(self) -> str:
        return self.esperar_visible(self.ALERTA_ERROR).text
