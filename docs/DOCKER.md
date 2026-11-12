# SIGR en contenedores Linux

Entorno completo y reproducible para desarrollo y pruebas de calidad: la aplicación, SQL Server y las herramientas de prueba, todo en contenedores Linux. Con un solo comando cualquiera puede levantar el sistema y ejecutar las pruebas sin instalar .NET, SQL Server, Python ni Chrome.

> **Solo para desarrollo y QA.** Las credenciales de ejemplo y la siembra de la clave de demostración no deben usarse en producción.

## Requisitos
- Docker Desktop (Windows o macOS con WSL 2) o Docker Engine (Linux), en modo **contenedores Linux**.
- Al menos **4 GB de RAM** asignados a Docker (SQL Server necesita mínimo 2 GB).
- Puertos libres: `8080` (aplicación) y `14330` (SQL Server). Se pueden cambiar en `.env` (ver `.env.example`).

## Arquitectura
```
┌────────────┐   ┌──────────┐      ┌──────────────┐
│ db-init    │──▶│    db    │◀─────│     web      │◀── selenium-tests · newman · zap · k6
│ (una vez)  │   │ SQL 2022 │      │ ASP.NET 9    │       (perfiles de pruebas)
└────────────┘   └──────────┘      └──────────────┘
```
| Servicio | Imagen | Función |
|---|---|---|
| `db` | `mssql/server:2022-latest` | Base de datos SQL Server para Linux |
| `db-init` | `mssql/server:2022-latest` | Ejecuta los scripts `database/01` a `05` y termina |
| `web` | construida con el `Dockerfile` (SDK 9.0 → runtime `aspnet:9.0`) | La aplicación, sin privilegios de root, en el puerto 8080 |
| `selenium-tests` | Python 3.12 + Chromium | Pruebas funcionales de interfaz |
| `newman` | `postman/newman:alpine` | Pruebas de la API REST |
| `zap` | `ghcr.io/zaproxy/zaproxy:stable` | Escaneo de seguridad base de OWASP ZAP |
| `k6` | `grafana/k6` | Pruebas de carga y rendimiento |

La base de datos se **recrea en cada arranque** (`01_schema.sql` borra y crea `SIGR`), así que cada ejecución de pruebas parte de datos limpios.

## Uso
```bash
# 1. Levantar sistema y base de datos (la primera vez descarga imágenes)
docker compose up -d --build
#    Aplicación: http://localhost:8080      Salud: http://localhost:8080/health

# 2. Pruebas funcionales de interfaz  → tests/evidencia/selenium/ (reporte.html, capturas)
docker compose --profile tests run --rm selenium-tests

# 3. Pruebas de la API               → tests/evidencia/postman/resultado.json
docker compose --profile tests run --rm newman

# 4. Escaneo de seguridad (ZAP)       → tests/evidencia/zap/informe_zap.html
docker compose --profile security run --rm zap

# 5. Rendimiento (k6)                 → tests/evidencia/k6/resumen.json
docker compose --profile performance run --rm k6

# Ver registros y apagar
docker compose logs -f web
docker compose down -v          # -v borra también los volúmenes
```
Usuarios ficticios (clave `Sigr2026!`): `admin@sigr.test`, `carlos.mesero@sigr.test`, `maria.lopez@correo.test`, `cocina@sigr.test`.

## Cómo interpretar los resultados
- **Selenium:** 2 casos fallan **a propósito** (`S-04` y `S-05`): documentan los hallazgos H-01 y H-02 del registro. El resto debe pasar.
- **Newman:** 18 peticiones y 40 aserciones, todas en verde.
- **ZAP:** el escaneo base es *pasivo* (no ataca); revisa en el informe las advertencias, por ejemplo cabeceras de seguridad ausentes. Se ejecuta con `-I`, por lo que las advertencias no hacen fallar el comando.
- **k6:** los umbrales (`thresholds`) están en `tests/k6/carga.js`: menos del 1 % de errores, p95 de lecturas < 500 ms y p95 de login < 1500 ms. Si alguno se incumple, k6 termina con error.

## Configuración
| Variable | Valor por defecto | Uso |
|---|---|---|
| `SA_PASSWORD` | `Sigr#Qa2026!` | Contraseña de `sa` en SQL Server |
| `SIGR_PORT` | `8080` | Puerto de la aplicación |
| `SQL_PORT` | `14330` | Puerto de SQL Server (solo en `127.0.0.1`) |
| `Demo__SembrarPasswords` | `true` (en el compose) | Reemplaza los hashes ficticios por el de la clave de demostración. En producción debe estar apagada |
| `ConnectionStrings__SIGR` | cadena hacia `db` | Cadena de conexión de la aplicación |

Para conectarte a la base desde tu equipo (Azure Data Studio o SSMS): servidor `localhost,14330`, usuario `sa`, contraseña `SA_PASSWORD`, y marca *Trust server certificate*.

## Limitaciones conocidas
- **No llames `app` al servicio de la aplicación:** Chrome trata el nombre `app` como el dominio de nivel superior `.app` y fuerza HTTPS, así que las pruebas fallan con `ERR_SSL_PROTOCOL_ERROR`. Por eso el servicio se llama `web`.
- Los tests de registro crean usuarios `qa.*`. Dentro del contenedor no hay `sqlcmd`, así que no se limpian al terminar; no importa porque la base es efímera (`docker compose down -v`).
- La interfaz carga Bootstrap Icons y algunas imágenes de internet: sin conexión se ven sin íconos ni fotos, pero las pruebas siguen funcionando.
- El servicio `web` usa HTTP (puerto 8080). En un despliegue real se debe publicar detrás de un proxy inverso con HTTPS.
