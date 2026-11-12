from selenium.webdriver.common.by import By

from .base_page import BasePage


class HomePage(BasePage):
    ruta = "/"
    BIENVENIDA = (By.CSS_SELECTOR, ".hero p.lead")
    ENLACES_MENU = (By.CSS_SELECTOR, ".nav-scrollable .nav-link")
    CERRAR_SESION = (By.XPATH, "//button[contains(., 'Cerrar sesión')]")

    def texto_bienvenida(self) -> str:
        return self.esperar_visible(self.BIENVENIDA).text

    def opciones_menu(self) -> list[str]:
        self.esperar_visible(self.ENLACES_MENU)
        return [e.text.strip() for e in self.driver.find_elements(*self.ENLACES_MENU) if e.text.strip()]

    def cerrar_sesion(self):
        self.driver.find_element(*self.CERRAR_SESION).click()
