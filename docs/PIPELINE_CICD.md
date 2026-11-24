# Pipeline CI/CD de SIGR

Archivo: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) · Override de staging: [`docker-compose.staging.yml`](../docker-compose.staging.yml)

## Etapas

| # | Etapa (trabajo) | Se ejecuta en | Qué hace | Falla si… |
|---|---|---|---|---|
| 1 | Integración | Pull Request / push | Dispara el flujo | — |
| 2–3 | `unitarias` | PR, push, manual | Compila en Release y corre 126 pruebas xUnit con cobertura | Compila mal o falla una prueba |
| 4 | `analisis` | PR, push, manual | `dotnet list package --vulnerable` y analizadores Roslyn | Hay paquetes vulnerables o más de 10 avisos (umbral) |
| 5 | `empaquetar` | PR, push, manual | Construye la imagen Docker; la **publica en GHCR** si no es un PR (`sha-<commit>`) | No compila la imagen |
| 6 | `e2e` | PR, push, manual | Playwright, Selenium y Newman contra el sistema en contenedores | Falla una prueba de interfaz o API |
| 7–8 | `staging` | push, etiqueta, manual | Descarga la imagen publicada (sin recompilar), la despliega con Docker Compose, prueba de humo (`/health`, `/api/platos`), OWASP ZAP y k6 | Falla el humo, ZAP o los umbrales de k6 |
| 9 | `produccion` | etiqueta `v*` o manual con `promover` | Promueve la **misma imagen** a las etiquetas `produccion` y `vX.Y.Z` | El revisor del entorno `production` no aprueba |

## Entornos de GitHub

- `staging`: sin aprobación.
- `production`: configurar *Required reviewers* (Settings → Environments). Sin esa regla el trabajo `produccion` no esperaría a nadie.

## Cómo probar el despliegue a staging en local

```bash
docker build -t sigr-staging:test .
export SIGR_IMAGE=sigr-staging:test
C="docker compose -f docker-compose.yml -f docker-compose.staging.yml"
$C up -d --no-build --pull never web
curl http://localhost:8080/health
$C --profile security run --rm zap
$C --profile performance run --rm k6
$C down -v
```

## Umbral de análisis estático

`UMBRAL=10` en el trabajo `analisis` es la deuda técnica actual (reglas CA1852, CA1310, CA1305, CA1822, CA1848). Solo debe bajar: al corregir avisos, reduzca el número.

## Limitaciones

- "Staging" es un entorno efímero dentro del ejecutor de GitHub (no hay servidor propio); valida que la imagen publicada arranca y pasa las pruebas.
- "Producción" promueve la imagen en el registro; el alojamiento real requeriría un servidor o servicio en la nube.
