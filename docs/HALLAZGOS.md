# Registro de hallazgos de calidad

Defectos y riesgos encontrados al auditar SIGR con pruebas automatizadas. Cada hallazgo tiene un identificador estable para citarlo en los informes y en los commits.

**Severidad:** Alta (acceso indebido o pérdida de datos) · Media (datos inválidos o error visible) · Baja (endurecimiento o mejora).
**Estado:** Abierto · Corregido (con commit o rama) · A revisar.

## Defectos del sistema

| ID | Hallazgo | Severidad | Detectado por | Estado | Corrección |
|---|---|---|---|---|---|
| H-01 | `POST /account/register` acepta un correo sin formato válido: solo se valida con `type=email` en el navegador | Media | Selenium `test_s04` | **Corregido** | Commit `c70e5fd` (rama `fix/validacion-registro`); `test_s04` pasa a verde |
| H-02 | `POST /account/register` acepta un nombre vacío o de solo espacios: solo existe `required` en el navegador | Media | Selenium `test_s05` | **Corregido** | Commit `c70e5fd` (rama `fix/validacion-registro`); `test_s05` pasa a verde |
| H-03 | Un pedido sin ítems puede avanzar hasta *Entregado* (el cobro sí lo bloquea con «el pedido no tiene ítems») | Media | Observado al probar el módulo de pagos | **Corregido** | Commit `c70e5fd`: `AvanzarAsync` y `CocinaAvanzarAsync` exigen al menos un ítem. Prueba Selenium `test_s06` |
| H-04 | Un `PasswordHash` con formato inválido hacía que el login respondiera **500** en vez de rechazar al usuario | Media | Newman en Docker (tras recrear la base con hashes ficticios) | **Corregido** | Commit `4865555` (`fix(auth)`) |
| H-05 | Faltan cabeceras de seguridad HTTP (`X-Content-Type-Options`, `Permissions-Policy`, `Cross-Origin-*`) y la política CSP es inexistente o permisiva (comodín y `unsafe-inline`) | Media | OWASP ZAP (reglas 10021, 10063, 90004, 10055) | **Abierto** | Middleware de cabeceras de seguridad (rama a definir, p. ej. `fix/cabeceras-seguridad`) |
| H-06 | El recurso de Bootstrap Icons se carga desde una CDN sin atributo de integridad (SRI) | Media | OWASP ZAP (regla 90003) | **Abierto** | Agregar `integrity` y `crossorigin` al `<link>`, o alojar la fuente localmente |
| H-08 | En Linux/Docker los precios se mostraban como `¤4,500` en lugar de `$ 4.500`: la app dependía de la cultura del sistema operativo | Media | Playwright (capturas del flujo del carrito) | **Corregido** | `UseRequestLocalization` con cultura fija `es-CO` en `Program.cs` (rama `test/e2e-playwright`) |
| H-07 | ZAP reporta «User Controllable HTML Element Attribute (Potential XSS)» en una página | Baja | OWASP ZAP (regla 10031) | **A revisar** | Probable falso positivo (Blazor codifica el valor reflejado de `ReturnUrl`); confirmar con una prueba manual |

## Observaciones informativas (sin acción obligatoria)
| Regla ZAP | Significado |
|---|---|
| 10049 Non-Storable Content | Las páginas dinámicas no son cacheables; es lo esperado |
| 10096 Timestamp Disclosure | Valores que parecen marcas de tiempo en recursos estáticos |
| 10109 Modern Web Application | Informa que la app es una SPA/Blazor |
| 10111 / 10112 | Identifica el formulario de login y la gestión de sesión |

## Resultados que SÍ pasan (controles verificados)
- Inyección SQL en el login: rechazada (consultas parametrizadas). Selenium S-01.
- Rutas protegidas sin sesión: redirigen al login; la API responde 401. Selenium S-02 y Newman API-06/18.
- Un Cliente no accede a pantallas de Administrador: 403 en la API y «acceso denegado» en la UI. Selenium S-03 y Newman API-10.
- La clave mínima de 6 caracteres se valida en el navegador **y** en el servidor. Selenium F-07a/b.
- ZAP baseline: **0 fallos**, 57 reglas superadas, 11 advertencias.

## Cómo agregar un hallazgo
1. Asigna el siguiente `H-0N`.
2. Registra: descripción, severidad, quién lo detectó (herramienta o caso), estado y la rama que lo corrige.
3. Cita el ID en el commit de la corrección: `fix(registro): valida correo y nombre en el servidor (H-01, H-02)`.
4. Cuando la prueba que lo detectó pase a verde, cambia el estado a **Corregido** con el hash del commit.
