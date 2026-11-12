# Entregas académicas — Pruebas y Calidad del Software

Mapa de lo que pide el curso, dónde está la evidencia y qué falta. Sirve para organizar el trabajo del equipo.

**Curso:** Pruebas y Calidad del Software · Ingeniería de Software · Universidad Tecnológica del Oriente
**Docente:** Carlos Carrascal Avendaño · **Repositorio:** https://github.com/kmilogirva/sigr

## Resumen
| # | Entrega | Modalidad | Formato | Estado | Evidencia |
|---|---|---|---|---|---|
| 1 | Línea base del código (SIGR) | Individual | PDF APA | ✅ Entregable generado | Etiqueta `v0.1.0-baseline` (commit `4b4e7f3`); PDF `Taller_LineaBase_SIGR.pdf` |
| 2 | Taller: scripts automatizados (formulario web + API) | Individual | PDF APA | ✅ Entregable generado | PDF `Taller_Scripts_Automatizados_JuanCamiloGiraldoValencia.pdf`; PR #1 y #2 |
| 3 | **Taller grupal: automatización de pruebas funcionales (segunda entrega)** | **Equipo** | **PDF, máx. 15 páginas sin portada** | 🔄 En construcción | Ver más abajo |

> Los PDF son entregables locales y **no se versionan** en el repositorio (`docs/*.pdf` está en `.gitignore`).

## Entrega 3 — Taller grupal (segunda entrega)
**Enunciado:** en equipos, automatizar un conjunto de pruebas funcionales para una pequeña aplicación o módulo web usando una de las herramientas propuestas. Guía paso a paso, evidencias del funcionamiento y reflexión grupal. Es la **continuación** del informe anterior: da solución a los hallazgos y amplía el alcance y las herramientas.

| Dato | Valor |
|---|---|
| Archivo | `Taller_Automatizacion_[NombreGrupo].pdf` |
| Rúbrica | `RAA2_U2_Prueba_calidad_software` |
| Extensión | Máximo 15 páginas **sin contar la portada** |
| Nombre del grupo | _por definir_ |
| Fecha de entrega | _por definir_ |

### Contenido mínimo exigido y dónde está
| Requisito | Cumplimiento | Evidencia en el repositorio |
|---|---|---|
| Scripts automatizados funcionales | ✅ | `tests/selenium/`, `tests/postman/`, `tests/k6/`, contenedor `zap` |
| Evidencias: código | ✅ | Extractos de los scripts en el informe |
| Evidencias: capturas | ✅ | `tests/evidencia/` (se regenera con los comandos de `PRUEBAS_DE_CALIDAD.md`) |
| Evidencias: resultados | ✅ | Tabla de resultados vigentes en `PRUEBAS_DE_CALIDAD.md` |
| Guía paso a paso | 🔄 | `PRUEBAS_DE_CALIDAD.md` y `DOCKER.md`; se redacta en el informe |
| Reflexión grupal | 🔄 | Insumo en `LECCIONES_APRENDIDAS.md`; falta redactarla con el equipo |
| Formato PDF y APA | ⏳ | Se genera al cerrar la fase 11 |
| Documento de documentación | ✅ | Este conjunto de archivos en `docs/` |

### Esquema propuesto del informe (15 páginas)
| Sección | Páginas | Fuente |
|---|---|---|
| 1. Introducción, objetivos y alcance (continuación del informe anterior) | 1,0 | Este documento |
| 2. Sistema bajo prueba y entorno en contenedores | 1,5 | `DOCKER.md` |
| 3. Estrategia y herramientas (pirámide de pruebas) | 1,0 | `PRUEBAS_DE_CALIDAD.md` |
| 4. Pruebas funcionales de interfaz (Selenium y Playwright) | 2,0 | `tests/selenium/`, `tests/playwright/` |
| 5. Pruebas de API (Newman y contrato OpenAPI) | 1,5 | `tests/postman/` |
| 6. Pruebas unitarias y de integración | 2,0 | fases 3 y 4 |
| 7. Seguridad (ZAP y dependencias) | 1,5 | `HALLAZGOS.md` |
| 8. Rendimiento (k6) | 1,0 | `tests/k6/` |
| 9. Integración continua y cobertura | 1,0 | fases 5 y 6 |
| 10. Hallazgos y soluciones (antes y después) | 1,0 | `HALLAZGOS.md` |
| 11. Reflexión grupal y lecciones aprendidas | 1,0 | `LECCIONES_APRENDIDAS.md` |
| 12. Conclusiones | 0,5 | — |
| **Total** | **15,0** | Confirmar con el docente si las referencias cuentan dentro del límite |

### Equipo y responsabilidades _(completar)_
| Integrante | Responsabilidad sugerida | Fases |
|---|---|---|
| Juan Camilo Giraldo Valencia | Coordinación, Docker, integración y Git | 0, 1, 5, 12 |
| _por definir_ | Pruebas unitarias e integración | 3, 4, 6 |
| _por definir_ | Pruebas de interfaz y accesibilidad | 7, 10 |
| _por definir_ | Seguridad y rendimiento | 8, 9 |
| _todos_ | Redacción del informe y reflexión | 11 |

### Checklist de cierre
- [ ] Nombre del grupo, integrantes y fecha definidos
- [x] Hallazgos H-01 a H-03 corregidos y sus pruebas en verde (Selenium 26/26)
- [ ] Pruebas unitarias e integración con resultados
- [ ] Pipeline de integración continua ejecutado (verde y rojo)
- [ ] Informe en APA con figuras y tablas numeradas
- [ ] PDF de máximo 15 páginas sin portada y con el nombre correcto
- [ ] Reflexión grupal redactada entre todos
- [ ] Etiqueta `v0.2.0-qa` creada

## Entregas anteriores (referencia rápida)
- **Línea base:** módulos (autenticación, menú, pedidos, reservas, caja y reportes), 23 commits, etiqueta `v0.1.0-baseline`. Documentos: `README.md`, `CHANGELOG.md`, `LICENSE.txt`, `docs/MANUAL_DESPLIEGUE.md`.
- **Scripts automatizados (individual):** 25 casos Selenium (23 pasan, 2 fallan por H-01 y H-02) y 18 peticiones Newman con 40 aserciones. PR #1 (API) y #2 (suite).
