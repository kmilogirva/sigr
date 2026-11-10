# Manual de despliegue — SIGR

## 1. Requisitos
| Componente | Versión |
|---|---|
| .NET SDK / runtime | 9.0 |
| SQL Server | 2019+ (Express/LocalDB válidos) |
| Navegador | Edge, Chrome o Firefox actuales |

## 2. Base de datos
1. Ejecutar `database/01_schema.sql` (**borra y recrea** la base `SIGR`).
2. Ejecutar `database/02_seed.sql` (datos ficticios).

Usar `-I` (QUOTED_IDENTIFIER) y `-f 65001` (UTF-8) en `sqlcmd`; en SSMS abrir el archivo como UTF-8.

### Modelo de datos
```
Roles 1─* Usuarios 1─* Reservas *─1 Mesas 1─* Pedidos 1─* DetallePedido *─1 Platos *─1 Categorias
                  └──* Pedidos (mesero)            └─* Pagos        CierresCaja *─1 Usuarios
```
| Tabla | Propósito |
|---|---|
| Roles, Usuarios | Autenticación y autorización |
| Categorias, Platos | Menú |
| Mesas, Reservas | Salón y reservas (`UQ MesaId+FechaHora`) |
| Pedidos, DetallePedido | Comandas; `Subtotal` es columna calculada |
| Pagos, CierresCaja | Cobros y cierre diario |
| vw_VentasDiarias, vw_PlatosMasVendidos | Reportes |
| sp_RecalcularTotalPedido, sp_CerrarCaja | Procedimientos de apoyo |

## 3. Configuración
Cadena de conexión en `src/SIGR.Web/appsettings.json` (clave `ConnectionStrings:SIGR`). En equipos distintos usar `dotnet user-secrets` o la variable de entorno `ConnectionStrings__SIGR`.

## 4. Ejecución local
```bash
cd src/SIGR.Web
dotnet run
```
Abrir la URL que muestra la consola (por defecto http://localhost:5063).

## 5. Publicación
```bash
dotnet publish src/SIGR.Web -c Release -o publish
```
Desplegar `publish/` en IIS (Hosting Bundle .NET 9) o ejecutar `dotnet SIGR.Web.dll` detrás de un proxy inverso. Definir `ASPNETCORE_ENVIRONMENT=Production` y la cadena de conexión por variable de entorno.

## 6. Notas de seguridad de la línea base
- Contraseñas con `PasswordHasher` de ASP.NET Core Identity.
- Formularios protegidos con antiforgery; redirecciones de login restringidas a rutas locales.
- Sembrado de la clave de demostración **solo** en Development. En producción, crear los usuarios reales y eliminar los datos ficticios.

## 7. Verificación rápida
1. Iniciar sesión como `admin@sigr.test` / `Sigr2026!`.
2. Abrir *Reportes*: deben verse ventas de los últimos días.
3. Como mesero, crear un pedido en una mesa libre, agregar ítems, avanzar hasta *Entregado* y cobrar.
4. Como cliente, crear una reserva a futuro.
