# Lecciones aprendidas

Errores y descubrimientos del proceso de automatización. Sirven de insumo para la reflexión del informe: la mayoría de los fallos iniciales fueron **defectos de las propias pruebas o del entorno**, no del sistema.

| # | Qué pasó | Causa real | Qué aprendimos |
|---|---|---|---|
| L-01 | Cinco pruebas de rutas protegidas fallaban | La app usa el parámetro `ReturnUrl` y la prueba buscaba `returnUrl` (mayúsculas) | Una prueba que falla puede estar mal escrita: se investiga antes de culpar al sistema |
| L-02 | El script de k6 reportaba 96 % de errores en `/api/reportes/ventas` | k6 **borra las cookies entre iteraciones** por defecto; tras la primera vuelta no había sesión | Hay que conocer los valores por defecto de la herramienta (`noCookiesReset: true`) |
| L-03 | Chrome dentro de Docker fallaba con `ERR_SSL_PROTOCOL_ERROR` | El servicio se llamaba `app`; Chrome trata ese nombre como el dominio de nivel superior `.app`, que fuerza HTTPS (HSTS precargado) | Un nombre de host aparentemente inocente puede romper una prueba. Se renombró a `web` |
| L-04 | Tras ejecutar `docker compose run`, el login devolvía 500 | `run` vuelve a ejecutar `db-init`, que recrea la base con hashes ficticios mientras la app ya estaba corriendo | Las dependencias entre servicios también se reinician. Llevó a descubrir el defecto H-04 y a que el login repare la clave de demostración |
| L-05 | La app se caía al arrancar con «Login failed for user sa» | Condición de carrera: SQL Server tardaba unos segundos tras recrear la base | El arranque debe tolerar a sus dependencias: reintentos y política de reinicio |
| L-06 | Un clic por JavaScript no agregaba el ítem al pedido | El clic por JS no dispara el evento `blur`; Blazor enlaza los campos al evento `change` | En Blazor conviene usar clics reales de Selenium y esperar el re-render |
| L-07 | Las pruebas de Selenium tardaban minutos o se colgaban | Procesos de Chrome huérfanos de una ejecución interrumpida | Cerrar siempre el navegador (`fixture`) y no matar procesos por nombre de forma indiscriminada |
| L-08 | Al abrir `reporte.html` de pytest-html la tabla salía vacía | El informe se renderiza con JavaScript desde `file://` | Para el informe académico se generó una tabla propia desde el XML |
| L-09 | La prueba S-06 fallaba con `StaleElementReference` y luego con «timeout» al crear un pedido | Blazor Server vuelve a pintar el formulario al conectar su circuito; un cambio hecho antes se pierde | El Page Object repite la selección hasta que el botón se habilita: esperar al **estado** de la UI, no solo a que exista el elemento |
| L-10 | Los precios salían como `¤4.500` en Docker pero bien en Windows | La cultura del proceso en un contenedor Linux es la invariante; el formato de moneda dependía del sistema operativo | Un entorno nuevo revela supuestos ocultos: la cultura debe fijarse en la aplicación (H-08) |

## Buenas prácticas que quedaron
- Probar la **capa del servidor** saltándose el navegador (`noValidate`), no solo lo que ve el usuario.
- **Datos de prueba separados** del código y limpieza automática de lo que las pruebas crean.
- Evidencia **automática** (capturas numeradas y captura al fallar).
- Una **base limpia por ejecución** (Docker) hace las pruebas repetibles.
- Registrar cada hallazgo con ID, severidad y estado en un solo lugar (`HALLAZGOS.md`).
