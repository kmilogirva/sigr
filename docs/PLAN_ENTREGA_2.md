# Plan de trabajo — Segunda entrega (calidad y pruebas de SIGR)

Flujo de siempre: una rama por entrega, salida desde `develop`, Pull Request de vuelta a `develop`, Conventional Commits y un issue por rama.
La segunda entrega es la **continuación** del informe anterior: da solución a los hallazgos y amplía el alcance y las herramientas (ver [ENTREGAS_ACADEMICAS.md](ENTREGAS_ACADEMICAS.md)).

**Estado:** ✅ hecho · 🔄 en curso o parcial · ⏳ pendiente

## Fase 0 — Preparación
| Paso | Acción | Estado |
|---|---|---|
| 0.1 | Fusionar los PR #1 (API) y #2 (suite) a `develop` | ✅ 11/11 |
| 0.2 | Crear un issue de GitHub por cada rama del plan | ⏳ |
| 0.3 | Confirmar Docker Desktop | ✅ Docker 28.3.2, motor Linux, 8 GB asignados |

## Trabajo previo que habilita el plan (no figuraba como fase)
| Rama | Contenido | Estado |
|---|---|---|
| `feature/inventario` | Insumos, recetas, kardex y descuento automático | ✅ |
| `feature/pagos-avanzados` | Cupones, propina, cuenta dividida y recibo | ✅ |
| `feature/gestion-usuarios` | Alta de personal, roles, activar/desactivar | ✅ |
| `feature/cocina` | Rol Cocinero y pantalla de comandas | ✅ |
| `feature/dockerizacion` | Contenedores Linux, SQL Server y perfiles de prueba | ✅ |
| `docs/modulos-nuevos` | README, CHANGELOG y manual de los módulos | ✅ |

## Fases y ramas
| # | Rama | Qué se hace | Herramientas | Evidencia para el informe | Estado |
|---|---|---|---|---|---|
| 1 | `fix/validacion-registro` | Corregir H-01, H-02 y H-03 en el servidor | .NET | Tests S-04 y S-05 de rojo a verde; nueva S-06 | ✅ Selenium 26/26 en Linux (commits `c70e5fd`, `ef3ba31`) |
| 2 | `feature/openapi` | Publicar el contrato OpenAPI/Swagger | Microsoft.AspNetCore.OpenApi | `openapi.json`, pantalla Swagger | ⏳ |
| 3 | `test/unitarias` | Lógica pura: `PagoService`, `Validaciones`, flujo de estados | xUnit, NSubstitute, FluentAssertions, Bogus | Tabla de casos y resultados | ⏳ |
| 4 | `test/integracion` | Servicios y API contra una base limpia | WebApplicationFactory, Testcontainers | Resultados y tiempos | ⏳ |
| 5 | `ci/github-actions` | Pipeline: compilar, unitarias, integración, Selenium y Newman en cada PR | GitHub Actions + SQL Server | Captura del pipeline verde y de uno rojo | ⏳ |
| 6 | `test/cobertura` | Cobertura y pruebas de mutación | Coverlet, ReportGenerator, Stryker.NET | Informe de cobertura | ⏳ |
| 7 | `test/e2e-playwright` | Login y flujo del carrito; comparar con Selenium | Playwright | Trazas, capturas, tabla comparativa | ⏳ |
| 8 | `test/seguridad-zap` | Escaneo y corrección de hallazgos | OWASP ZAP, `dotnet list package --vulnerable` | Informe ZAP antes/después | 🔄 escaneo hecho; faltan correcciones (H-05, H-06, H-07) y dependencias |
| 9 | `test/rendimiento-k6` | Humo, carga, estrés y reservas concurrentes | k6 | Gráficos de latencia y errores | 🔄 humo y carga hechos; faltan estrés y reservas concurrentes |
| 10 | `test/accesibilidad` | Auditoría de accesibilidad (opcional) | axe-core / Lighthouse | Reporte con puntaje | ⏳ |
| 11 | `docs/informe-entrega-2` | Informe APA consolidado (máx. 15 páginas sin portada) | PDF | El entregable | ⏳ |
| 12 | `release/0.2.0` | Congelar versión, CHANGELOG y etiqueta | Git | Etiqueta `v0.2.0-qa` | ⏳ |

## Orden y dependencias
```
0 ─┬─ 1 fix registro ─┬─ 3 unitarias ── 4 integración ─┬─ 5 CI ─┐
   │                  └─ 7 playwright ── 10 accesib.   └─ 6 cobertura
   └─ 2 openapi ──────┬─ 8 ZAP                                   ├─ 11 informe ── 12 release
                      └─ 9 k6 ────────────────────────────────────┘
```
**Camino mínimo sólido** (poco tiempo): 0, 1, 3, 5, 8 y 11.

## Convenciones
- **Commits:** `fix(registro): valida correo y nombre en el servidor (H-01, H-02)`, `test(unitarias): prueba el cálculo de descuento`, `ci: agrega pipeline de GitHub Actions`.
- **Definición de terminado de cada rama:** compila, sus pruebas pasan, el README de `tests/` está actualizado, tiene Pull Request y dejó evidencia guardada (capturas o reportes).
- **Pull Request:** uno por rama, con resumen, cómo probar y resultados.

## Riesgos
| Riesgo | Mitigación |
|---|---|
| Pruebas de interfaz inestables | Esperas explícitas, clics reales, Playwright y pocos flujos esenciales |
| Falta de tiempo | Recortar las fases 6, 9 y 10, las más prescindibles |
| Informe de 15 páginas máximo con mucho contenido | Presupuesto de páginas por sección (ver `ENTREGAS_ACADEMICAS.md`) y evidencia densa: tablas y figuras compactas |
| Trabajo grupal sin repartir | Asignar responsables por fase (tabla en `ENTREGAS_ACADEMICAS.md`) |
