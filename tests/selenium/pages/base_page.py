from selenium.webdriver.remote.webdriver import WebDriver
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.support.ui import WebDriverWait

import config


class BasePage:
    """Funciones comunes a todos los Page Objects: navegación, esperas explícitas y validación HTML5."""

    ruta = "/"

    def __init__(self, driver: WebDriver):
        self.driver = driver
        self.wait = WebDriverWait(driver, config.TIMEOUT)

    def abrir(self):
        self.driver.get(config.BASE_URL + self.ruta)
        return self

    def esperar_visible(self, locator):
        return self.wait.until(EC.visibility_of_element_located(locator))

    def esperar_url_contiene(self, texto):
        self.wait.until(EC.url_contains(texto))

    def hay_elemento(self, locator) -> bool:
        return len(self.driver.find_elements(*locator)) > 0

    def validez(self, locator) -> dict:
        """Estado de validación HTML5 del campo (lo que decide el navegador antes de enviar)."""
        el = self.driver.find_element(*locator)
        return self.driver.execute_script(
            "const v=arguments[0].validity;"
            "return {valid:v.valid,valueMissing:v.valueMissing,tooShort:v.tooShort,typeMismatch:v.typeMismatch};", el)

    def desactivar_validacion_html(self):
        """Simula a un cliente que se salta el navegador, para probar la validación del servidor."""
        self.driver.execute_script("document.querySelector('form').noValidate = true;")
