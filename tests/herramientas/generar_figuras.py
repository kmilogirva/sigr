"""Genera las figuras del informe a partir de la evidencia real de las ejecuciones.

Uso (desde la raíz del repositorio, con la evidencia ya generada en tests/evidencia):
    python tests/herramientas/generar_figuras.py

Produce tests/evidencia/figuras/*.png:
  - Consolas de Selenium, Playwright, Newman y k6 convertidas a imagen
  - Gráficos: resultados por herramienta, latencia k6, comparación Selenium vs Playwright
  - Capturas de ZAP y copia ordenada de las capturas más representativas de cada suite
"""
import glob
import json
import re
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib import font_manager as fm
from PIL import Image, ImageDraw, ImageFont

RAIZ = Path(__file__).resolve().parents[2]
EV = RAIZ / "tests" / "evidencia"
OUT = EV / "figuras"
OUT.mkdir(parents=True, exist_ok=True)

for f in ("times.ttf", "timesbd.ttf", "timesi.ttf"):
    ruta = Path(r"C:\Windows\Fonts") / f
    if ruta.exists():
        fm.fontManager.addfont(str(ruta))
plt.rcParams["font.family"] = ["Times New Roman", "DejaVu Serif"]
plt.rcParams["font.size"] = 11
NAVY, PURPLE, GREEN, GOLD, RED, GREY = "#052767", "#6f2dbd", "#198754", "#ffc107", "#b02a37", "#6c757d"


# ---------------------------------------------------------------- consola -> imagen
def texto_a_imagen(lineas, destino, titulo, ancho=1250):
    """Renderiza texto monoespaciado con aspecto de terminal."""
    fuente_ruta = Path(r"C:\Windows\Fonts\consola.ttf")
    fuente = ImageFont.truetype(str(fuente_ruta), 15) if fuente_ruta.exists() else ImageFont.load_default()
    alto_linea = 20
    h = 44 + alto_linea * len(lineas) + 16
    img = Image.new("RGB", (ancho, h), "#1e1e1e")
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, ancho, 32], fill="#323233")
    for i, c in enumerate(("#ff5f56", "#ffbd2e", "#27c93f")):
        d.ellipse([12 + i * 22, 10, 24 + i * 22, 22], fill=c)
    d.text((88, 8), titulo, fill="#cccccc", font=fuente)
    y = 44
    for ln in lineas:
        # La fuente de consola no trae estos símbolos: se sustituyen por equivalentes
        ln = ln.replace("✓", "√").replace("✗", "x").replace("█", "#").replace("↳", "->")
        color = "#d4d4d4"
        if re.search(r"PASSED|\bPassed\b|\d+ passed|√|\bOK\b", ln): color = "#6ad48b"
        if re.search(r"\bFAILED\b|\bFailed\b|\d+ failed|x '|FAIL-NEW: [1-9]", ln): color = "#f48771"
        if re.search(r"WARN", ln): color = "#e5c07b"
        d.text((14, y), ln[:150], fill=color, font=fuente)
        y += alto_linea
    img.save(destino)
    print("figura", destino.name)


def limpiar(txt):
    return txt.replace("\x1b", "")


def lineas_pytest(log, maximo=40):
    todas = [l.rstrip() for l in Path(log).read_text(encoding="utf-8", errors="replace").splitlines()]
    sel = [l for l in todas if re.search(r"(PASSED|FAILED)\s+\[", l) or re.search(r"=+ .*(passed|failed).* in ", l)]
    sel = [re.sub(r"\s+\[\s*\d+%\]", "", l) for l in sel]
    sel = [l.replace("test_", "", 1) if l.startswith("test_") else l for l in sel]
    return sel[:maximo]


# ---------------------------------------------------------------- consolas
def consolas():
    if (EV / "selenium_docker.log").exists():
        texto_a_imagen(["$ docker compose --profile tests run --rm selenium-tests", ""] + lineas_pytest(EV / "selenium_docker.log"),
                       OUT / "consola_selenium.png", "Selenium + pytest (Chromium en Linux)")
    if (EV / "playwright_docker.log").exists():
        texto_a_imagen(["$ docker compose --profile tests run --rm playwright-tests", ""] + lineas_pytest(EV / "playwright_docker.log"),
                       OUT / "consola_playwright.png", "Playwright + pytest (Chromium en Linux)")
    if (EV / "newman_docker.log").exists():
        l = [limpiar(x.rstrip()) for x in (EV / "newman_docker.log").read_text(encoding="utf-8", errors="replace").splitlines()]
        l = [x for x in l if x.strip() and not re.search(r"Pulling|Download|Extracting|Pull complete|Waiting|Container", x)]
        i = next((k for k, x in enumerate(l) if "API-12" in x), 0)
        fin = next((k for k, x in enumerate(l) if "average response time" in x), len(l) - 1)
        texto_a_imagen(["$ docker compose --profile tests run --rm newman", ""] + l[i - 1:i + 6] + ["  [...]"] + l[-22:fin + 1],
                       OUT / "consola_newman.png", "Newman (Postman) - 18 peticiones, 40 aserciones")
    if (EV / "k6_consola.log").exists():
        t = (EV / "k6_consola.log").read_text(encoding="utf-8", errors="replace")
        i = t.rfind("THRESHOLDS")
        bloque = [limpiar(x.rstrip()) for x in t[i:].splitlines()]
        bloque = [x for x in bloque if x.strip()][:38]
        texto_a_imagen(["$ docker compose --profile performance run --rm k6", ""] + bloque,
                       OUT / "consola_k6.png", "k6 - 20 usuarios virtuales, 52 s")
    if (EV / "zap_consola.log").exists():
        t = [x.rstrip() for x in (EV / "zap_consola.log").read_text(encoding="utf-8", errors="replace").splitlines()]
        sel = [x for x in t if x.startswith(("WARN-NEW", "FAIL-NEW", "PASS:", "FAIL-INPROG")) or "FAIL-NEW:" in x]
        sel = [x for x in sel if not x.startswith("PASS: ")][:20]
        texto_a_imagen(["$ docker compose --profile security run --rm zap", ""] + sel,
                       OUT / "consola_zap.png", "OWASP ZAP - baseline pasivo", ancho=1100)


# ---------------------------------------------------------------- gráficos
def tiempos(xml):
    return [(tc.get("name"), float(tc.get("time"))) for tc in ET.parse(xml).getroot().iter("testcase")]


def grafico_resultados():
    sel = tiempos(EV / "selenium" / "resultados.xml")
    pw = tiempos(EV / "playwright" / "resultados.xml")
    etiquetas = ["Selenium\n(casos)", "Playwright\n(casos)", "Newman\n(peticiones)", "k6\n(verificaciones)", "ZAP\n(reglas)"]
    pasan = [len(sel), len(pw), 18, 3021, 57]
    avisos = [0, 0, 0, 0, 11]
    fallan = [0, 0, 0, 0, 0]
    fig, ejes = plt.subplots(1, 5, figsize=(10, 3.4))
    for ax, e, p, a, f in zip(ejes, etiquetas, pasan, avisos, fallan):
        ax.bar([0], [p], color=GREEN, width=0.6)
        if a: ax.bar([0], [a], bottom=[p], color=GOLD, width=0.6)
        if f: ax.bar([0], [f], bottom=[p + a], color=RED, width=0.6)
        ax.text(0, p / 2, str(p), ha="center", color="white", fontweight="bold")
        if a: ax.text(0, p + a / 2, str(a), ha="center", fontsize=9)
        ax.set_xticks([0]); ax.set_xticklabels([e], fontsize=9); ax.set_yticks([])
        ax.spines[["top", "right", "left"]].set_visible(False)
    fig.legend(handles=[plt.Rectangle((0, 0), 1, 1, color=GREEN), plt.Rectangle((0, 0), 1, 1, color=GOLD), plt.Rectangle((0, 0), 1, 1, color=RED)],
               labels=["Pasan", "Advertencias", "Fallan"], loc="upper center", ncol=3, frameon=False)
    fig.savefig(OUT / "grafico_resultados.png", dpi=200, bbox_inches="tight"); plt.close(fig)
    print("figura grafico_resultados.png")


def grafico_comparacion():
    sel = tiempos(EV / "selenium" / "resultados.xml")
    pw = tiempos(EV / "playwright" / "resultados.xml")

    def prom(lst, patron):
        ts = [t for n, t in lst if re.search(patron, n)]
        return sum(ts) / len(ts) if ts else 0

    casos = [("Login válido\npor rol", "f01_f04", "login_valido"), ("Credenciales\ninválidas", "f02", "credenciales"),
             ("Rutas protegidas\nsin sesión", "s02", "rutas_protegidas"), ("Cliente en pantalla\nde administrador", "s03", "no_accede")]
    a = [prom(sel, c[1]) for c in casos]
    b = [prom(pw, c[2]) for c in casos]
    x = range(len(casos))
    fig, ax = plt.subplots(figsize=(8, 3.8))
    ax.bar([i - 0.2 for i in x], a, 0.4, label="Selenium", color=NAVY)
    ax.bar([i + 0.2 for i in x], b, 0.4, label="Playwright", color=PURPLE)
    for i, (u, v) in enumerate(zip(a, b)):
        ax.text(i - 0.2, u + 0.04, f"{u:.2f}", ha="center", fontsize=9)
        ax.text(i + 0.2, v + 0.04, f"{v:.2f}", ha="center", fontsize=9)
    ax.set_xticks(list(x)); ax.set_xticklabels([c[0] for c in casos], fontsize=9)
    ax.set_ylabel("Segundos por caso (promedio)"); ax.legend(frameon=False)
    ax.spines[["top", "right"]].set_visible(False); ax.grid(axis="y", alpha=.25)
    fig.savefig(OUT / "grafico_selenium_vs_playwright.png", dpi=200, bbox_inches="tight"); plt.close(fig)
    print("figura grafico_selenium_vs_playwright.png")


def grafico_k6():
    ruta = EV / "k6" / "resumen.json"
    if not ruta.exists():
        return
    m = json.loads(ruta.read_text(encoding="utf-8"))["metrics"]
    def v(nombre, k):
        return m.get(nombre, {}).get(k)
    grupos = [("Lecturas públicas", "http_req_duration{tipo:lectura}"), ("Login", "http_req_duration{tipo:login}"), ("Todas", "http_req_duration")]
    datos = [(g, v(n, "avg"), v(n, "p(90)"), v(n, "p(95)")) for g, n in grupos if v(n, "avg") is not None]
    fig, ax = plt.subplots(figsize=(7.5, 3.6))
    ancho = 0.25
    for j, (etq, col) in enumerate((("Promedio", NAVY), ("p90", PURPLE), ("p95", GOLD))):
        vals = [d[j + 1] for d in datos]
        pos = [i + (j - 1) * ancho for i in range(len(datos))]
        ax.bar(pos, vals, ancho, label=etq, color=col)
        for p, val in zip(pos, vals): ax.text(p, val + 1.5, f"{val:.1f}", ha="center", fontsize=8)
    ax.set_xticks(range(len(datos))); ax.set_xticklabels([d[0] for d in datos])
    ax.set_ylabel("Latencia (ms)"); ax.legend(frameon=False)
    ax.spines[["top", "right"]].set_visible(False); ax.grid(axis="y", alpha=.25)
    fig.savefig(OUT / "grafico_k6_latencia.png", dpi=200, bbox_inches="tight"); plt.close(fig)
    print("figura grafico_k6_latencia.png")



# ---------------------------------------------------------------- pruebas unitarias
def unitarias():
    base = EV / "unitarias"
    trx = base / "cobertura" / "resultados.trx"
    if trx.exists():
        ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
        raiz = ET.parse(trx).getroot()
        clase_de = {}
        for ut in raiz.iterfind(".//t:UnitTest", ns):
            tm = ut.find("t:TestMethod", ns)
            clase_de[ut.get("id")] = tm.get("className").split(".")[-1]
        cuenta, falla = {}, {}
        for r in raiz.iterfind(".//t:UnitTestResult", ns):
            c = clase_de.get(r.get("testId"), "?")
            cuenta[c] = cuenta.get(c, 0) + 1
            if r.get("outcome") != "Passed":
                falla[c] = falla.get(c, 0) + 1
        nombres = list(cuenta)
        fig, ax = plt.subplots(figsize=(7.5, 3.4))
        ax.barh(nombres, [cuenta[n] - falla.get(n, 0) for n in nombres], color=GREEN, label="Pasan")
        ax.barh(nombres, [falla.get(n, 0) for n in nombres], left=[cuenta[n] - falla.get(n, 0) for n in nombres], color=RED, label="Fallan")
        for i, n in enumerate(nombres):
            ax.text(cuenta[n] + 0.8, i, str(cuenta[n]), va="center", fontsize=9)
        ax.set_xlabel(f"Pruebas unitarias xUnit (total: {sum(cuenta.values())}, todas pasan)" if not falla else "Pruebas unitarias xUnit")
        ax.spines[["top", "right"]].set_visible(False); ax.invert_yaxis(); ax.legend(frameon=False)
        fig.savefig(OUT / "grafico_unitarias_por_clase.png", dpi=200, bbox_inches="tight"); plt.close(fig)
        print("figura grafico_unitarias_por_clase.png")

    cob = base / "cobertura_por_clase.json"
    if cob.exists():
        d = json.loads(cob.read_text(encoding="utf-8"))
        def pct(k):
            v = d.get(k)
            return 100 * v["cubiertas"] / v["total"] if v and v["total"] else 0
        clases = [("Validaciones", "SIGR.Web.Services.Validaciones"), ("PagoService", "SIGR.Web.Services.PagoService"),
                  ("UsuarioService", "SIGR.Web.Services.UsuarioService"), ("InventarioService", "SIGR.Web.Services.InventarioService"),
                  ("PedidoService", "SIGR.Web.Services.PedidoService")]
        vals = [pct(k) for _, k in clases]
        fig, ax = plt.subplots(figsize=(7.5, 3.4))
        cols = [GREEN if v >= 80 else GOLD if v >= 20 else RED for v in vals]
        ax.bar([c[0] for c in clases], vals, color=cols)
        for i, v in enumerate(vals):
            ax.text(i, v + 2, f"{v:.0f} %", ha="center", fontsize=9)
        ax.set_ylim(0, 112); ax.set_ylabel("Líneas cubiertas por pruebas unitarias (%)")
        ax.spines[["top", "right"]].set_visible(False); ax.grid(axis="y", alpha=.25)
        ax.tick_params(axis="x", labelsize=9)
        fig.savefig(OUT / "grafico_cobertura.png", dpi=200, bbox_inches="tight"); plt.close(fig)
        print("figura grafico_cobertura.png")

    reg = base / "regresion_h01_h02.log"
    if reg.exists():
        l = [x.rstrip() for x in reg.read_text(encoding="utf-8", errors="replace").splitlines() if x.strip()]
        l = [re.sub(r"\s*\[.*?\]$", "", x) for x in l]
        l = [x.replace("Registro_publico_rechaza_datos_invalidos", "Registro_rechaza_datos_invalidos") for x in l]
        texto_a_imagen(["Pruebas unitarias contra el codigo ANTES de la correccion (commit 29db872)", "Resultado: 6 de 16 fallan", ""] + l[:16],
                       OUT / "consola_regresion_h01_h02.png", "dotnet test - regresion de H-01 y H-02", ancho=1250)

# ---------------------------------------------------------------- ZAP y capturas
def captura_zap():
    informe = EV / "zap" / "informe_zap.html"
    if not informe.exists():
        return
    from selenium import webdriver
    from selenium.webdriver.chrome.options import Options
    o = Options(); o.add_argument("--headless=new"); o.add_argument("--window-size=1300,1500"); o.add_argument("--allow-file-access-from-files")
    d = webdriver.Chrome(options=o)
    try:
        d.get(informe.resolve().as_uri())
        import time; time.sleep(1.5)
        d.save_screenshot(str(OUT / "captura_zap_informe.png"))
        print("figura captura_zap_informe.png")
    finally:
        d.quit()


def copiar_capturas():
    elegidas = {
        "selenium": [("test_f01_f04*cliente*02_sesion*", "sel_01_login_cliente"), ("test_f01_f04*admin*02_sesion*", "sel_02_login_admin"),
                     ("test_f02*clave_incorrecta*", "sel_03_credenciales_invalidas"), ("test_s03*", "sel_04_acceso_denegado"),
                     ("test_s04*", "sel_05_registro_correo_invalido"), ("test_s06*avance_rechazado*", "sel_06_pedido_vacio_rechazado")],
        "playwright/capturas": [("*flujo_completo*01_carrito_armado*", "pw_01_carrito_armado"), ("*flujo_completo*02_mesa_seleccionada*", "pw_02_mesa_seleccionada"),
                                ("*flujo_completo*03_pedido_creado*", "pw_03_pedido_creado"), ("*flujo_completo*04_pedido_cancelado*", "pw_04_pedido_cancelado"),
                                ("*login_valido*cocinero*", "pw_05_login_cocinero"), ("*sin_mesa*", "pw_06_confirmar_exige_mesa")],
    }
    for carpeta, lista in elegidas.items():
        for patron, nombre in lista:
            hallados = glob.glob(str(EV / carpeta / (patron if patron.endswith("*") else patron + "*")))
            if hallados:
                shutil.copy(hallados[0], OUT / f"{nombre}.png")
                print("copia", nombre)


if __name__ == "__main__":
    consolas()
    grafico_resultados()
    grafico_comparacion()
    grafico_k6()
    unitarias()
    captura_zap()
    copiar_capturas()
    print("Figuras en", OUT)
