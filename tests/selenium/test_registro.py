"""Pruebas funcionales del formulario de registro de clientes (/registro).

Los usuarios creados usan el prefijo 'qa.' en el correo y se eliminan al finalizar la sesión de pruebas.
"""
import time

import pytest

import config
from conftest import correo_unico
from pages.home_page import HomePage
from pages.login_page import LoginPage
from pages.registro_page import RegistroPage


@pytest.mark.humo
@pytest.mark.parametrize("clave", ["abc123", "ClaveLarga#2026"], ids=["limite_6_caracteres", "clave_larga"])
def test_f05_registro_valido_y_posterior_inicio_de_sesion(driver, evidencia, clave):
    email = correo_unico("ok")
    reg = RegistroPage(driver).abrir().llenar("Usuario de Prueba QA", email, "3001234567", clave)
    evidencia("formulario_lleno")
    reg.enviar()

    login = LoginPage(driver)
    assert "Registro exitoso" in login.mensaje_exito()
    assert "registrado=1" in driver.current_url
    evidencia("registro_exitoso")

    # Flujo completo: la cuenta recién creada puede iniciar sesión y es un Cliente.
    login.escribir(email, clave).enviar()
    home = HomePage(driver)
    assert "Usuario de Prueba QA" in home.texto_bienvenida()
    assert "Cliente" in driver.find_element("css selector", ".hero").text
    evidencia("primer_inicio_de_sesion")


def test_f06_registro_con_correo_duplicado_es_rechazado(driver, evidencia):
    reg = RegistroPage(driver).registrar(
        nombre="Otra Maria", email=config.USUARIOS["cliente"]["email"], password="abc123")
    assert "Ya existe un usuario con ese correo" in reg.mensaje_error()
    evidencia("correo_duplicado")


def test_f07a_clave_de_5_caracteres_es_bloqueada_por_el_navegador(driver, evidencia):
    reg = RegistroPage(driver).abrir().llenar("Prueba QA", correo_unico("c5"), "", "abc12")
    reg.enviar()
    evidencia("envio_bloqueado")
    assert driver.current_url.endswith("/registro")
    assert reg.validez(reg.PASSWORD)["tooShort"] is True


@pytest.mark.seguridad
def test_f07b_servidor_rechaza_clave_de_5_caracteres_aunque_se_omita_el_navegador(driver, evidencia):
    reg = RegistroPage(driver).abrir()
    reg.desactivar_validacion_html()
    reg.llenar("Prueba QA", correo_unico("c5srv"), "", "abc12").enviar()
    assert "al menos 6 caracteres" in reg.mensaje_error()
    evidencia("rechazo_del_servidor")


def test_f09_campos_obligatorios_vacios_son_bloqueados_por_el_navegador(driver, evidencia):
    reg = RegistroPage(driver).abrir()
    reg.enviar()
    evidencia("envio_bloqueado")
    assert driver.current_url.endswith("/registro")
    for campo in (reg.NOMBRE, reg.EMAIL, reg.PASSWORD):
        assert reg.validez(campo)["valueMissing"] is True
    assert reg.validez(reg.TELEFONO)["valid"] is True, "el teléfono es opcional"


@pytest.mark.seguridad
def test_s04_servidor_rechaza_correo_sin_formato_valido(driver, evidencia):
    """Auditoría: el correo solo se valida en el navegador (type=email)."""
    reg = RegistroPage(driver).abrir()
    reg.desactivar_validacion_html()
    reg.llenar("Prueba QA", f"qa.sin-arroba-{int(time.time() * 1000)}", "", "abc123").enviar()
    evidencia("respuesta_del_servidor")
    assert "registrado=1" not in driver.current_url, "HALLAZGO: el servidor aceptó un correo sin formato"


@pytest.mark.seguridad
def test_s05_servidor_rechaza_nombre_vacio_o_solo_espacios(driver, evidencia):
    """Auditoría: el nombre solo se valida con el atributo required del navegador."""
    reg = RegistroPage(driver).abrir()
    reg.desactivar_validacion_html()
    reg.llenar("   ", correo_unico("nom"), "", "abc123").enviar()
    evidencia("respuesta_del_servidor")
    assert "registrado=1" not in driver.current_url, "HALLAZGO: el servidor aceptó un nombre vacío"
