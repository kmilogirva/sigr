# Bitácora del proyecto

Cronología de lo realizado, con las ramas y los commits de cada hito. Para el detalle de cada cambio, ver `CHANGELOG.md` y el historial de Git.

## 09/11/2026 — Línea base v0.1.0
| Hito | Rama / commit |
|---|---|
| Esquema SQL Server y datos ficticios | `docs/base-de-datos` · `1dfb00b` |
| Estructura de la solución Blazor | `b1dffff` |
| Capa de datos con Dapper | `feature/acceso-datos` · `d1e5cb1` (+ fix `23cb38e`) |
| Autenticación con cookies, registro y roles | `feature/autenticacion` · `a0820da` |
| Menú digital y CRUD de platos | `feature/menu-digital` · `884976a` (+ fix `2a2a507`) |
| Pedidos con flujo de estados y cobro | `feature/pedidos` · `2e121db` |
| Reservas con control de disponibilidad | `feature/reservas` · `0a1339c` |
| Cierre de caja y reportes | `feature/caja-reportes` · `3b6e173` |
| README, CHANGELOG y manual | `docs/linea-base` · `11b5481` |
| Congelación y etiqueta | `release/0.1.0` · `e9111ff` → `v0.1.0-baseline` en `4b4e7f3` |
| Rediseño visual y pedido del cliente | `develop` · `fe14bd6` |

## 11/11/2026 — Automatización, módulos nuevos y Docker
| Hito | Rama / commit |
|---|---|
| API REST de solo lectura | `feature/api-rest` · `c9650cf` (PR #1) |
| Suite Selenium + Postman | `test/automatizacion` · `dbd0a65` (PR #2) |
| Informe de la línea base y del taller individual | PDF locales (no versionados) |
| Inventario con descuento automático | `feature/inventario` · `42108f6` |
| Pagos: cupones, propina, cuenta dividida y recibo | `feature/pagos-avanzados` · `e18f5d2` |
| Gestión de usuarios | `feature/gestion-usuarios` · `0dd3504` |
| Cocina y rol Cocinero | `feature/cocina` · `5bc64b8` |
| Documentación de los módulos | `docs/modulos-nuevos` · `7c20838` |
| Integración en `develop` y subida | `develop` · `a9798ea` |
| Corrección del login ante hashes inválidos (H-04) | `feature/dockerizacion` · `4865555` |
| Dockerización con contenedores Linux | `feature/dockerizacion` · `7184b20`, merge `0c011b3` |
| Primera ejecución en contenedores: Selenium 23/2, Newman 40/0, k6 sin errores, ZAP 0 fallos y 11 advertencias | evidencia en `tests/evidencia/` |
| Documentación académica | `docs/documentacion-academica` · `1f935e6` |
| **Fase 1:** corrección de H-01, H-02 y H-03; Selenium pasa de 23/2 a 26/0 | `fix/validacion-registro` · `c70e5fd`, `ef3ba31` |

## Próximos pasos
Ver el estado fase por fase en [PLAN_ENTREGA_2.md](PLAN_ENTREGA_2.md). Lo siguiente en el camino mínimo: **fase 1** (corregir H-01 a H-03), **fase 3** (pruebas unitarias) y **fase 5** (integración continua).
