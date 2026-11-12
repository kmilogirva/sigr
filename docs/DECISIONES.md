# Decisiones técnicas

Registro breve de decisiones de diseño y de proceso, con su motivo y sus consecuencias. Sirve para justificarlas en los informes.

| ID | Decisión | Motivo | Consecuencia |
|---|---|---|---|
| D-01 | Acceso a datos con **Dapper** y SQL explícito, sin Entity Framework | Control total del SQL, transacciones claras y consultas parametrizadas | Más código de mapeo; los cambios de esquema son scripts SQL |
| D-02 | **API REST mínima** bajo `/api`, con la misma cookie de sesión (sin JWT) | SIGR no tenía API y se necesitaba probar servicios REST; reutiliza la autenticación existente | Las rutas `/api` responden 401/403 en vez de redirigir al login |
| D-03 | El inventario se **descuenta al agregar el ítem** (no al iniciar la preparación) | Simple y consistente: quitar un ítem o cancelar devuelve el stock; sin stock el plato se rechaza al pedirlo | Un pedido Pendiente ya reserva insumos |
| D-04 | `Pedidos.Total` es el **subtotal**; descuento y propina van en columnas aparte | Total a pagar = Total − Descuento + Propina sin perder el subtotal original | Las ventas diarias se miden netas de descuento; la propina no es ingreso |
| D-05 | Módulos nuevos como **scripts SQL incrementales y re-ejecutables** (`03` a `05`) | Se pueden aplicar sobre una base existente sin borrar datos | Hay que ejecutarlos en orden al crear una base desde cero |
| D-06 | **Docker con contenedores Linux**: SQL Server 2022, `db-init` con `sqlcmd` y la app en `aspnet:9.0` | Entorno reproducible y base limpia en cada ejecución de pruebas | Requiere Docker y ≥ 4 GB de RAM |
| D-07 | El servicio de la aplicación se llama **`web`**, no `app` | Chrome fuerza HTTPS en el dominio `.app` (HSTS precargado) | Ver lección L-03 |
| D-08 | La siembra de la clave de demostración se activa con **`Demo__SembrarPasswords`** | No depender de `Development` en entornos de QA; no debe estar activa en producción | El login repara hashes ficticios al vuelo si el modo demo está activo |
| D-09 | Los hallazgos H-01 y H-02 se dejaron abiertos a propósito y se corrigieron en la fase 1 | Demostrar el ciclo rojo → verde: la prueba que detecta el defecto es la que confirma la corrección | S-04 y S-05 pasaron de rojo a verde (`c70e5fd`) |
| D-10 | Selenium se mantiene y se **suma Playwright** para comparar | Evidenciar criterios de elección entre herramientas | Dos suites de interfaz con tablas comparativas en el informe |
| D-11 | Los **PDF de entregas no se versionan** | Son entregables locales y pesan varios MB | `docs/*.pdf` está en `.gitignore` |
| D-12 | La evidencia generada (`tests/evidencia/`) **no se versiona** | Se regenera con un comando y cambia en cada ejecución | La documentación indica cómo reproducirla |
