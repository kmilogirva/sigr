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
| Funcional de interfaz | Selenium + pytest (Page Object) | Login, registro, control de acceso por rol, cierre de sesión | `tests/selenium/` | ✅ Hecho |
| API REST | Postman + Newman | Menú público, login/logout, autorización 401/403, reportes y valores límite | `tests/postman/` | ✅ Hecho |
| Rendimiento | k6 | Humo (1 usuario) y carga (20 usuarios) sobre lecturas, login y reportes | `tests/k6/carga.js` | ✅ Hecho |
| Seguridad | OWASP ZAP (baseline pasivo) | Cabeceras, cookies, recursos externos, reflexión | contenedor `zap` | ✅ Hecho (correcciones pendientes) |
| Unitarias | xUnit, NSubstitute, FluentAssertions | `PagoService` (descuento, propina, cupón), `Validaciones`, flujo de estados | — | ⏳ Pendiente |
| Integración | WebApplicationFactory, Testcontainers | Servicios y API contra una base limpia | — | ⏳ Pendiente |
| E2E moderno | Playwright | Login y flujo del carrito de pedido | — | ⏳ Pendiente |
| Cobertura y mutación | Coverlet, ReportGenerator, Stryker.NET | Calidad de las pruebas | — | ⏳ Pendiente |
| Accesibilidad | axe-core / Lighthouse | Contraste, etiquetas, teclado | — | ⏳ Pendiente |
| Integración continua | GitHub Actions | Ejecutar todo en cada Pull Request | — | ⏳ Pendiente |

## Cómo ejecutarlas

### Con Docker (recomendado: base limpia y sin instalar nada)
```bash
docker compose up -d --build                                  # sistema + base de datos
docker compose --profile tests run --rm selenium-tests        # interfaz   -> tests/evidencia/selenium/
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
| Selenium (25 casos) | **23 pasan, 2 fallan** | Los 2 fallos son los hallazgos H-01 y H-02 (intencionales). Duración: 76 s |
| Newman (18 peticiones) | **40 aserciones, 0 fallos** | Promedio ~42 ms por petición |
| k6 (carga hasta 20 usuarios, 52 s) | **0 % de errores, 100 % de verificaciones** | 600 iteraciones, 2.421 peticiones. p95 lecturas: **4,6 ms** · p95 login: **87 ms** |
| ZAP baseline | **0 fallos, 11 advertencias, 57 reglas superadas** | Advertencias H-05, H-06 y H-07 |

Los resultados en Windows (contra la app local) coinciden: Selenium 23/2 y Newman 18/40/0.

### Umbrales de aceptación (k6)
| Métrica | Umbral | Resultado |
|---|---|---|
| Peticiones fallidas | < 1 % | 0 % ✅ |
| Verificaciones (`checks`) | > 99 % | 100 % ✅ |
| p95 de lecturas públicas | < 500 ms | 4,6 ms ✅ |
| p95 de login | < 1.500 ms | 87 ms ✅ |

## Datos de prueba
- Usuarios ficticios (clave `Sigr2026!`): `admin@sigr.test`, `carlos.mesero@sigr.test`, `maria.lopez@correo.test`, `cocina@sigr.test`.
- Los tests de registro crean usuarios con correo `qa.*`; en Windows se eliminan al terminar (con `sqlcmd`), en Docker la base es efímera.
- Cupones de demostración: `BIENVENIDA10` (vigente), `PROMO5000` (vigente), `NAVIDAD2025` (vencido), `UNICO` (agotado).

## Limitaciones
- No hay aún pruebas unitarias ni de integración, ni cobertura medida.
- ZAP se ejecuta en modo baseline pasivo (no ataca activamente ni autentica).
- La suite de interfaz depende de textos de la UI; si cambian, hay que actualizar los Page Objects.
