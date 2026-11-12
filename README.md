# SIGR — Sistema Integral de Gestión de Restaurante

Aplicación web (Blazor Web App, .NET 9, SQL Server + Dapper) para gestionar pedidos, reservas, menú, caja y reportes de ventas.

## Módulos de la línea base
1. **Autenticación** — login con cookies; roles Cliente, Mesero y Administrador; registro público de clientes.
2. **Menú digital** — vista pública y CRUD de platos y categorías (Administrador).
3. **Pedidos** — apertura por mesa, ítems, flujo Pendiente → EnPreparacion → Listo → Entregado → Pagado; refresco cada 5 s.
4. **Reservas** — por fecha y hora, con control de capacidad y de choques de horario.
5. **Caja y reportes** — cierre de caja diario, ventas diarias y platos más vendidos.

### Módulos añadidos después de la línea base
6. **Inventario** — insumos, recetas con costo y margen, kardex de movimientos y alerta de stock bajo. Cada plato pedido descuenta sus insumos; sin stock suficiente el pedido se rechaza, y quitar un ítem o cancelar devuelve el stock.
7. **Pagos avanzados** — cupones de descuento (porcentaje o monto, con vigencia y límite de usos), propina, cuenta dividida entre varios métodos de pago y recibo imprimible.
8. **Gestión de usuarios** — alta de personal, cambio de rol, activar/desactivar y restablecer contraseña, con validación completa en el servidor.
9. **Cocina** — nuevo rol Cocinero y pantalla de comandas (nuevas, en preparación, listas) con alertas por antigüedad y tiempo promedio de preparación.

## Requisitos
- .NET SDK 9.0
- SQL Server 2019+ (Express sirve: `localhost\SQLEXPRESS`)
- Git

## Instalación
```bash
git clone https://github.com/<usuario>/sigr.git
cd sigr

# 1. Base de datos (crea SIGR con datos ficticios)
sqlcmd -S "localhost\SQLEXPRESS" -E -I -f 65001 -i database/01_schema.sql
sqlcmd -S "localhost\SQLEXPRESS" -E -I -f 65001 -i database/02_seed.sql
# Módulos añadidos (cada script es re-ejecutable y se puede aplicar sobre una base existente)
sqlcmd -S "localhost\SQLEXPRESS" -E -I -f 65001 -i database/03_inventario.sql
sqlcmd -S "localhost\SQLEXPRESS" -E -I -f 65001 -i database/04_pagos_avanzados.sql
sqlcmd -S "localhost\SQLEXPRESS" -E -I -f 65001 -i database/05_cocina.sql

# 2. Ejecutar (cadena de conexión por defecto en appsettings.json)
cd src/SIGR.Web
dotnet run
```
Si tu instancia es otra, sobreescribe la cadena sin tocar el repositorio:
```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SIGR" "Server=<tu-servidor>;Database=SIGR;Trusted_Connection=True;TrustServerCertificate=True"
```

## Usuarios ficticios de prueba (clave `Sigr2026!`)
| Rol | Email |
|---|---|
| Administrador | admin@sigr.test |
| Mesero | carlos.mesero@sigr.test |
| Cocinero | cocina@sigr.test |
| Cliente | maria.lopez@correo.test |

En modo *Development* la app reemplaza los hashes ficticios del seed por el hash real de esa clave al arrancar. No existe ese comportamiento en producción.

## Estructura
```
database/   scripts SQL (esquema base 01-02 y módulos 03-05)
docs/       guía de Git y manual de despliegue
src/SIGR.Web/
  Data/        fábrica de conexiones
  Models/      entidades
  Services/    lógica y acceso a datos (Dapper)
  Components/  páginas y layout Blazor
```
Toda la documentación (calidad, hallazgos, plan de entregas, bitácora y decisiones) está indexada en [docs/README.md](docs/README.md). Entorno en contenedores: [docs/DOCKER.md](docs/DOCKER.md).

Flujo de ramas y commits: [docs/GUIA_GIT.md](docs/GUIA_GIT.md) · Despliegue: [docs/MANUAL_DESPLIEGUE.md](docs/MANUAL_DESPLIEGUE.md).

## Licencia
MIT — ver [LICENSE.txt](LICENSE.txt).
