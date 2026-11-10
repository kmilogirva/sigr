# SIGR — Sistema Integral de Gestión de Restaurante

Aplicación web (Blazor, .NET 9) para gestionar pedidos, reservas, menú, caja y reportes de ventas.

## Módulos de la línea base
1. Autenticación (Cliente, Mesero, Administrador)
2. Menú digital (CRUD de platos y categorías)
3. Pedidos en tiempo real
4. Reservas por fecha y hora
5. Cierre de caja y reportes diarios

## Requisitos
- .NET SDK 9.0
- SQL Server 2019+ (o LocalDB / Express)
- Git

## Instalación
```bash
git clone https://github.com/<usuario>/sigr.git
cd sigr

# 1. Base de datos (en SSMS o con sqlcmd)
sqlcmd -S localhost -E -i database/01_schema.sql
sqlcmd -S localhost -E -i database/02_seed.sql

# 2. Cadena de conexión (no se sube al repositorio)
cd src/SIGR.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SIGR" "Server=localhost;Database=SIGR;Trusted_Connection=True;TrustServerCertificate=True"

# 3. Ejecutar
dotnet run
```

## Usuarios ficticios de prueba
| Rol | Email |
|---|---|
| Administrador | admin@sigr.test |
| Mesero | carlos.mesero@sigr.test |
| Cliente | maria.lopez@correo.test |

## Estructura
```
database/   scripts SQL (esquema + datos ficticios)
docs/       guías y documentación técnica
src/        código fuente (SIGR.Web)
```
Flujo de ramas y commits: ver [docs/GUIA_GIT.md](docs/GUIA_GIT.md).

## Licencia
MIT — ver [LICENSE.txt](LICENSE.txt).
