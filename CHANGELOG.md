# Changelog
Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/). Versionado semántico.

## [0.1.0-baseline] - 09/11/2026
### Añadido
- Estructura inicial de la solución Blazor (`SIGR.sln`, `SIGR.Web`). — `chore`
- Esquema SQL Server y datos ficticios (`database/`). — rama `docs/base-de-datos`
- Capa de acceso a datos con Dapper, modelos y fábrica de conexiones. — rama `feature/acceso-datos`
- Autenticación con cookies, registro de clientes y roles. — rama `feature/autenticacion`
- Menú digital público y CRUD de platos y categorías. — rama `feature/menu-digital`
- Pedidos con flujo de estados, cobro y refresco automático. — rama `feature/pedidos`
- Reservas por fecha y hora con control de disponibilidad. — rama `feature/reservas`
- Cierre de caja diario y reportes de ventas. — rama `feature/caja-reportes`
- Documentación: README, guía de Git, manual de despliegue. — rama `docs/linea-base`

### Corregido
- Escape del backslash en la cadena de conexión (`fix/connection-string`).
- Conflicto de nombre entre el servicio inyectado y el componente `Menu` (`fix/menu-nombre`).
- Opciones `QUOTED_IDENTIFIER` y UTF-8 al ejecutar los scripts con `sqlcmd`.
