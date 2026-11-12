# Documentación de SIGR

Índice de toda la documentación del proyecto, técnica y académica.

## Técnica
| Documento | Para qué sirve |
|---|---|
| [`../README.md`](../README.md) | Visión general, módulos, instalación y usuarios de prueba |
| [`MANUAL_DESPLIEGUE.md`](MANUAL_DESPLIEGUE.md) | Despliegue, scripts de base de datos y verificación rápida |
| [`DOCKER.md`](DOCKER.md) | Entorno en contenedores Linux, comandos y configuración |
| [`GUIA_GIT.md`](GUIA_GIT.md) | Flujo de ramas, Conventional Commits y plan de entregas |
| [`../CHANGELOG.md`](../CHANGELOG.md) | Historial de cambios por versión |
| [`DECISIONES.md`](DECISIONES.md) | Decisiones de diseño y de proceso con su motivo |

## Calidad y pruebas
| Documento | Para qué sirve |
|---|---|
| [`PRUEBAS_DE_CALIDAD.md`](PRUEBAS_DE_CALIDAD.md) | Estrategia, herramientas, cómo ejecutarlas y resultados vigentes |
| [`HALLAZGOS.md`](HALLAZGOS.md) | Registro de defectos y riesgos con severidad y estado |
| [`LECCIONES_APRENDIDAS.md`](LECCIONES_APRENDIDAS.md) | Errores y descubrimientos del proceso (insumo de la reflexión) |
| [`../tests/README.md`](../tests/README.md) | Casos de prueba y su ejecución |

## Académica
| Documento | Para qué sirve |
|---|---|
| [`ENTREGAS_ACADEMICAS.md`](ENTREGAS_ACADEMICAS.md) | Qué pide cada entrega, estado, evidencia, esquema del informe y equipo |
| [`PLAN_ENTREGA_2.md`](PLAN_ENTREGA_2.md) | Plan de fases y ramas con su estado |
| [`BITACORA.md`](BITACORA.md) | Cronología de hitos con ramas y commits |

## Mapa del repositorio
```
database/    scripts SQL: 01-02 base, 03 inventario, 04 pagos, 05 cocina
docs/        documentación (este índice)
src/SIGR.Web aplicación Blazor (Components, Services, Endpoints, Models, Data)
tests/       selenium/ · postman/ · k6/ · evidencia/ (generada)
Dockerfile · docker-compose.yml · .env.example
```

## Mantenimiento de esta documentación
- Cada hito termina con una entrada en `BITACORA.md`.
- Cada defecto nuevo se registra en `HALLAZGOS.md` con su ID.
- Al terminar una fase, se actualiza su estado en `PLAN_ENTREGA_2.md` y los resultados en `PRUEBAS_DE_CALIDAD.md`.
