# Pruebas automatizadas de SIGR

Suite de pruebas funcionales que audita el sistema desde afuera, como lo haría un usuario o un cliente de la API.

| Carpeta | Herramienta | Qué prueba |
|---|---|---|
| `selenium/` | Python + pytest + Selenium (patrón Page Object) | Formularios web: login, registro y control de acceso por rol |
| `postman/` | Postman + Newman | API REST: menú público, autenticación, autorización y reportes |
| `k6/` | k6 | Rendimiento: humo y carga sobre lecturas, login y reportes |
| `evidencia/` | (generada, no se versiona) | Capturas, reportes HTML/XML/JSON y salida de consola |

## Ejecución en contenedores Linux (recomendada)
Levanta el sistema con una base limpia y ejecuta cada tipo de prueba sin instalar nada. Detalle en [`docs/DOCKER.md`](../docs/DOCKER.md).
```bash
docker compose up -d --build
docker compose --profile tests run --rm selenium-tests     # interfaz
docker compose --profile tests run --rm newman             # API
docker compose --profile security run --rm zap             # seguridad (OWASP ZAP)
docker compose --profile performance run --rm k6           # rendimiento
```
Resultados vigentes, hallazgos y estrategia: [`docs/PRUEBAS_DE_CALIDAD.md`](../docs/PRUEBAS_DE_CALIDAD.md) y [`docs/HALLAZGOS.md`](../docs/HALLAZGOS.md).

## Ejecución local en Windows
### Requisitos
- App en ejecución: `cd src/SIGR.Web && dotnet run --launch-profile https` (https://localhost:7072) con la base `SIGR` y datos ficticios.
- Python 3.10+ y Chrome. `pip install -r tests/selenium/requirements.txt`
- Node.js (solo para Newman): se usa con `npx newman`, sin instalación global.

## Ejecución
```bash
# Formularios (Selenium)
cd tests/selenium
python -m pytest                      # todo
python -m pytest -m humo              # solo pruebas rápidas
python -m pytest -m seguridad         # validación del servidor y control de acceso
SIGR_HEADLESS=0 python -m pytest -k f01   # ver el navegador

# API (Postman/Newman)
cd tests
npx newman run postman/SIGR_API.postman_collection.json -e postman/SIGR_local.postman_environment.json --insecure
```
Variables de entorno: `SIGR_BASE_URL`, `SIGR_HEADLESS`, `SIGR_TIMEOUT`, `SIGR_SQL_SERVER`.

Los tests de registro crean usuarios con correo `qa.*`; al terminar la sesión se eliminan con `sqlcmd`.

## Diseño de casos (formulario web)
| ID | Caso | Resultado esperado |
|---|---|---|
| F-01/F-04 | Login válido con cada rol | Entra al inicio y el menú lateral muestra solo lo permitido al rol |
| F-02 | Credenciales inválidas (3 variantes) | Mensaje "Correo o contraseña incorrectos" |
| F-03 | Campos vacíos | El navegador bloquea el envío |
| F-05 | Registro válido (límite de 6 caracteres y clave larga) | Mensaje de éxito y primer inicio de sesión como Cliente |
| F-06 | Correo duplicado | Mensaje de error |
| F-07a / F-07b | Clave de 5 caracteres | Bloqueada por el navegador **y** rechazada por el servidor |
| F-08 | Cerrar sesión | Sesión terminada; rutas protegidas vuelven a pedir login |
| F-09 | Campos obligatorios vacíos en registro | El navegador bloquea el envío; teléfono es opcional |
| S-01 | Inyección SQL en el login (3 cargas) | Rechazo sin autenticar |
| S-02 | Rutas protegidas sin sesión (5 rutas) | Redirección a `/login` conservando `ReturnUrl` |
| S-03 | Cliente abre pantalla de Administrador | Acceso denegado |
| S-04 | Correo sin formato válido saltando el navegador | El servidor lo rechaza con mensaje (H-01) |
| S-05 | Nombre vacío o solo espacios saltando el navegador | El servidor lo rechaza con mensaje (H-02) |
| S-06 | Avanzar un pedido sin ítems | Se rechaza: «el pedido no tiene ítems» (H-03) |

## Diseño de casos (API)
18 peticiones y 40 aserciones en `postman/`: menú público (API-01 a 05, con variables encadenadas), login y logout (API-06 a 09, 11, 17), autorización 401/403 (API-06, 10, 18), reportes y valores límite (API-12 a 16).

## Hallazgos de la auditoría
Registro completo en [`docs/HALLAZGOS.md`](../docs/HALLAZGOS.md). H-01, H-02 y H-03 se detectaron con estas pruebas y se corrigieron en `fix/validacion-registro`: los tests S-04, S-05 y S-06 pasaron de rojo a verde (Selenium: 26 de 26).
