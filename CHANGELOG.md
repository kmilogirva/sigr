# Changelog
Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/). Versionado semántico.

## [Sin publicar]
### Añadido
- Rediseño visual (Bootstrap Icons, hero, sidebar con íconos) y pantalla de acceso sin barra lateral. — `feat(ui)`
- Pedido autogestionado por el cliente: carrito, selección de mesa libre y seguimiento en «Mis pedidos». — `feat(ui)`
- Inventario: insumos, recetas, kardex y descuento automático por pedido (`database/03_inventario.sql`). — rama `feature/inventario`
- Pagos avanzados: cupones, propina, cuenta dividida y recibo (`database/04_pagos_avanzados.sql`). — rama `feature/pagos-avanzados`
- Gestión de usuarios con validación en el servidor. — rama `feature/gestion-usuarios`
- Cocina: rol Cocinero y pantalla de comandas (`database/05_cocina.sql`). — rama `feature/cocina`

### Cambiado
- Las ventas diarias (`vw_VentasDiarias`) se calculan netas de descuento.

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
