from selenium.webdriver.common.by import By

from .base_page import BasePage


class LoginPage(BasePage):
    ruta = "/login"
    EMAIL = (By.NAME, "email")
    PASSWORD = (By.NAME, "password")
    ENVIAR = (By.CSS_SELECTOR, "form button")
    ALERTA_ERROR = (By.CSS_SELECTOR, ".alert-danger")
    ALERTA_EXITO = (By.CSS_SELECTOR, ".alert-success")

    def escribir(self, email: str, password: str):
        self.esperar_visible(self.EMAIL).clear()
        self.driver.find_element(*self.EMAIL).send_keys(email)
        self.driver.find_element(*self.PASSWORD).clear()
        self.driver.find_element(*self.PASSWORD).send_keys(password)
        return self

    def enviar(self):
        self.driver.find_element(*self.ENVIAR).click()
        return self

    def iniciar_sesion(self, email: str, password: str):
        return self.abrir().escribir(email, password).enviar()

    def mensaje_error(self) -> str:
        return self.esperar_visible(self.ALERTA_ERROR).text

    def mensaje_exito(self) -> str:
        return self.esperar_visible(self.ALERTA_EXITO).text
