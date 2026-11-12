# Pruebas de calidad de SIGR

Estrategia, herramientas, cómo ejecutarlas y resultados vigentes. Los detalles de cada hallazgo están en [HALLAZGOS.md](HALLAZGOS.md); el entorno en contenedores, en [DOCKER.md](DOCKER.md).

## Estrategia
Se aplica la **pirámide de pruebas**: muchas pruebas rápidas y baratas en la base, pocas y lentas en la cima. Las pruebas actúan **desde afuera** del sistema (caja negra) y, donde importa, se saltan el navegador para auditar el servidor.

```
            /\            E2E / interfaz  ......... Selenium (hoy) · Playwright (previsto)
           /  \           API ................... Postman/Newman
          /----\          Seguridad ............. OWASP ZAP · dependencias (previsto)
         /      \         Rendimiento ........... k6
        /--------\        Integración ........... WebApplicationFactory + Testcontainers (previsto)
       /__________\       Unitarias ............. xUnit + NSubstitute + FluentAssertions (previsto)
```

## Matriz de pruebas
| Tipo | Herramienta | Qué cubre | Código | Estado |
|---|---|---|---|---|
| Funcional de interfaz | Selenium + pytest (Page Object) | Login, registro, pedidos del mesero, control de acceso por rol, cierre de sesión | `tests/selenium/` | ✅ Hecho |
| API REST | Postman + Newman | Menú público, login/logout, autorización 401/403, reportes y valores límite | `tests/postman/` | ✅ Hecho |
| Rendimiento | k6 | Humo (1 usuario) y carga (20 usuarios) sobre lecturas, login y reportes | `tests/k6/carga.js` | ✅ Hecho |
| Seguridad | OWASP ZAP (baseline pasivo) | Cabeceras, cookies, recursos externos, reflexión | contenedor `zap` | ✅ Hecho (correcciones pendientes) |
| Unitarias | xUnit, NSubstitute, FluentAssertions | `PagoService` (descuento, propina, cupón), `Validaciones`, flujo de estados | — | ⏳ Pendiente |
| Integración | WebApplicationFactory, Testcontainers | Servicios y API contra una base limpia | — | ⏳ Pendiente |
| E2E moderno | Playwright (Python) | Login, control de acceso por 4 roles y flujo completo del carrito de pedido del cliente | `tests/playwright/` | ✅ Hecho |
| Cobertura y mutación | Coverlet, ReportGenerator, Stryker.NET | Calidad de las pruebas | — | ⏳ Pendiente |
| Accesibilidad | axe-core / Lighthouse | Contraste, etiquetas, teclado | — | ⏳ Pendiente |
| Integración continua | GitHub Actions | Ejecutar todo en cada Pull Request | — | ⏳ Pendiente |

## Cómo ejecutarlas

### Con Docker (recomendado: base limpia y sin instalar nada)
```bash
docker compose up -d --build                                  # sistema + base de datos
docker compose --profile tests run --rm selenium-tests        # interfaz   -> tests/evidencia/selenium/
docker compose --profile tests run --rm playwright-tests      # E2E       -> tests/evidencia/playwright/
docker compose --profile tests run --rm newman                # API        -> tests/evidencia/postman/
docker compose --profile security run --rm zap                # seguridad  -> tests/evidencia/zap/
docker compose --profile performance run --rm k6              # rendimiento-> tests/evidencia/k6/
docker compose down -v
```
Cada `run` recrea la base de datos (comportamiento intencional: partir de datos limpios).

### En Windows, contra la app local
```bash
cd src/SIGR.Web && dotnet run --launch-profile https          # https://localhost:7072
cd tests/selenium && python -m pytest                          # interfaz
cd tests && npx newman run postman/SIGR_API.postman_collection.json -e postman/SIGR_local.postman_environment.json --insecure
```
Detalle de casos y marcas (`humo`, `seguridad`) en [`tests/README.md`](../tests/README.md).

## Resultados vigentes (11/11/2026, contenedores Linux, base limpia)
| Prueba | Resultado | Detalle |
|---|---|---|
| Selenium (26 casos) | **26 pasan, 0 fallan** | Tras corregir H-01, H-02 y H-03 (antes: 23 pasan y 2 fallan, de 25 casos). Duración: 56 s |
| Playwright (16 casos) | **16 pasan, 0 fallan** | Incluye el flujo del carrito (carrito → mesa → pedido → cancelación). Duración: 28 s |
| Newman (18 peticiones) | **40 aserciones, 0 fallos** | Promedio ~42 ms por petición |
| k6 (carga hasta 20 usuarios, 52 s) | **0 % de errores, 100 % de verificaciones** | 600 iteraciones, 2.421 peticiones. p95 lecturas: **4,6 ms** · p95 login: **87 ms** |
| ZAP baseline | **0 fallos, 11 advertencias, 57 reglas superadas** | El informe clasifica las alertas en 0 altas, 5 medias, 6 bajas y 7 informativas. Corresponden a H-05, H-06 y H-07 |

Antes de la fase 1, los resultados en Windows (contra la app local) coincidían con los de Linux: Selenium 23/2 y Newman 18/40/0.

### Umbrales de aceptación (k6)
| Métrica | Umbral | Resultado |
|---|---|---|
| Peticiones fallidas | < 1 % | 0 % ✅ |
| Verificaciones (`checks`) | > 99 % | 100 % ✅ |
| p95 de lecturas públicas | < 500 ms | 4,6 ms ✅ |
| p95 de login | < 1.500 ms | 87 ms ✅ |

## Comparación Selenium vs Playwright
Mismo sistema, mismo contenedor Linux y mismo Chromium. Tiempos medidos por caso (promedio, en segundos):

| Caso equivalente | Selenium 4.36 | Playwright 1.49 |
|---|---|---|
| Login válido por rol | 2,53 | 1,30 |
| Credenciales inválidas | 2,07 | 1,59 |
| Rutas protegidas sin sesión | 1,59 | 1,02 |
| Cliente en pantalla de administrador | 1,81 | 0,90 |
| Suite completa | 26 casos · 56,1 s | 16 casos · 28,0 s |

| Aspecto | Selenium | Playwright |
|---|---|---|
| Esperas | Explícitas, escritas a mano (`WebDriverWait`) | Auto-espera integrada y aserciones con reintento (`expect`) |
| Elementos obsoletos (`StaleElement`) | Frecuentes con Blazor; requirieron reintentos | No aparecen: los localizadores se resuelven en cada acción |
| Evidencia | Capturas escritas a mano en cada paso | Capturas y **trazas** (`trace.zip`) automáticas al fallar |
| Instalación en Docker | Chromium + chromedriver por `apt` | Imagen oficial con navegadores incluidos |
| Circuito de Blazor | Requiere reintentos | También requiere un reintento al hacer clic (ver `MenuPage.agregar`) |

**Conclusión:** para esta aplicación Playwright resultó más rápido y más estable, y con menos código de espera. Selenium sigue siendo válido y cubre más casos (registro y pedidos del mesero). La comparación por caso es la justa, porque las suites no tienen el mismo número de pruebas.

## Datos de prueba
- Usuarios ficticios (clave `Sigr2026!`): `admin@sigr.test`, `carlos.mesero@sigr.test`, `maria.lopez@correo.test`, `cocina@sigr.test`.
- Los tests de registro crean usuarios con correo `qa.*`; en Windows se eliminan al terminar (con `sqlcmd`), en Docker la base es efímera.
- Cupones de demostración: `BIENVENIDA10` (vigente), `PROMO5000` (vigente), `NAVIDAD2025` (vencido), `UNICO` (agotado).

## Figuras del informe
La evidencia visual se genera con un script reproducible, a partir de las ejecuciones reales:
```bash
python tests/herramientas/generar_figuras.py        # -> tests/evidencia/figuras/
```
| Figura | Archivo | Contenido |
|---|---|---|
| Consolas como imagen | `consola_selenium`, `consola_playwright`, `consola_newman`, `consola_k6`, `consola_zap` | Salida real de cada herramienta |
| Gráficos | `grafico_resultados`, `grafico_selenium_vs_playwright`, `grafico_k6_latencia` | Resultados por herramienta, comparación de tiempos y latencia de k6 |
| Informe ZAP | `captura_zap_informe` | Resumen de alertas por nivel de riesgo |
| Capturas de pasos | `sel_01` a `sel_06`, `pw_01` a `pw_06` | Selenium (login por rol, error, acceso denegado, validación del servidor, pedido vacío) y Playwright (flujo completo del carrito) |

## Limitaciones
- No hay aún pruebas unitarias ni de integración, ni cobertura medida.
- ZAP se ejecuta en modo baseline pasivo (no ataca activamente ni autentica).
- La suite de interfaz depende de textos de la UI; si cambian, hay que actualizar los Page Objects.
