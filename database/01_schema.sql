/* =====================================================================
   SIGR - Sistema Integral de Gestión de Restaurante
   Script 01: Esquema de base de datos (SQL Server 2019+)
   Ejecutar primero. Es re-ejecutable (recrea la BD).
   ===================================================================== */
USE master;
GO
IF DB_ID('SIGR') IS NOT NULL
BEGIN
    ALTER DATABASE SIGR SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SIGR;
END
GO
CREATE DATABASE SIGR;
GO
USE SIGR;
GO
SET QUOTED_IDENTIFIER ON;   -- requerido por la columna calculada persistida (sqlcmd lo trae en OFF)
GO

/* ---------- Módulo autenticación ---------- */
CREATE TABLE dbo.Roles (
    RolId       INT IDENTITY(1,1) CONSTRAINT PK_Roles PRIMARY KEY,
    Nombre      NVARCHAR(30) NOT NULL CONSTRAINT UQ_Roles_Nombre UNIQUE   -- Administrador, Mesero, Cliente
);

CREATE TABLE dbo.Usuarios (
    UsuarioId     INT IDENTITY(1,1) CONSTRAINT PK_Usuarios PRIMARY KEY,
    RolId         INT NOT NULL CONSTRAINT FK_Usuarios_Roles REFERENCES dbo.Roles(RolId),
    NombreCompleto NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(120) NOT NULL CONSTRAINT UQ_Usuarios_Email UNIQUE,
    Telefono      NVARCHAR(20)  NULL,
    PasswordHash  NVARCHAR(200) NOT NULL,
    Activo        BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT 1,
    FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_Usuarios_Fecha DEFAULT SYSDATETIME()
);

/* ---------- Módulo menú digital ---------- */
CREATE TABLE dbo.Categorias (
    CategoriaId INT IDENTITY(1,1) CONSTRAINT PK_Categorias PRIMARY KEY,
    Nombre      NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Categorias_Nombre UNIQUE,
    Descripcion NVARCHAR(200) NULL,
    Activa      BIT NOT NULL CONSTRAINT DF_Categorias_Activa DEFAULT 1
);

CREATE TABLE dbo.Platos (
    PlatoId     INT IDENTITY(1,1) CONSTRAINT PK_Platos PRIMARY KEY,
    CategoriaId INT NOT NULL CONSTRAINT FK_Platos_Categorias REFERENCES dbo.Categorias(CategoriaId),
    Nombre      NVARCHAR(80)  NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    Precio      DECIMAL(10,2) NOT NULL CONSTRAINT CK_Platos_Precio CHECK (Precio >= 0),
    Disponible  BIT NOT NULL CONSTRAINT DF_Platos_Disp DEFAULT 1,
    ImagenUrl   NVARCHAR(250) NULL,
    CONSTRAINT UQ_Platos_Cat_Nombre UNIQUE (CategoriaId, Nombre)
);

/* ---------- Mesas y reservas ---------- */
CREATE TABLE dbo.Mesas (
    MesaId    INT IDENTITY(1,1) CONSTRAINT PK_Mesas PRIMARY KEY,
    Numero    INT NOT NULL CONSTRAINT UQ_Mesas_Numero UNIQUE,
    Capacidad INT NOT NULL CONSTRAINT CK_Mesas_Cap CHECK (Capacidad > 0),
    Estado    NVARCHAR(15) NOT NULL CONSTRAINT DF_Mesas_Estado DEFAULT 'Libre'
              CONSTRAINT CK_Mesas_Estado CHECK (Estado IN ('Libre','Ocupada','Reservada'))
);

CREATE TABLE dbo.Reservas (
    ReservaId     INT IDENTITY(1,1) CONSTRAINT PK_Reservas PRIMARY KEY,
    ClienteId     INT NOT NULL CONSTRAINT FK_Reservas_Usuarios REFERENCES dbo.Usuarios(UsuarioId),
    MesaId        INT NOT NULL CONSTRAINT FK_Reservas_Mesas REFERENCES dbo.Mesas(MesaId),
    FechaHora     DATETIME2(0) NOT NULL,
    NumPersonas   INT NOT NULL CONSTRAINT CK_Reservas_Pers CHECK (NumPersonas > 0),
    Estado        NVARCHAR(15) NOT NULL CONSTRAINT DF_Reservas_Estado DEFAULT 'Pendiente'
                  CONSTRAINT CK_Reservas_Estado CHECK (Estado IN ('Pendiente','Confirmada','Cancelada','Completada')),
    Observaciones NVARCHAR(200) NULL,
    CONSTRAINT UQ_Reservas_Mesa_Fecha UNIQUE (MesaId, FechaHora)   -- evita doble reserva
);

/* ---------- Pedidos ---------- */
CREATE TABLE dbo.Pedidos (
    PedidoId    INT IDENTITY(1,1) CONSTRAINT PK_Pedidos PRIMARY KEY,
    MesaId      INT NOT NULL CONSTRAINT FK_Pedidos_Mesas REFERENCES dbo.Mesas(MesaId),
    MeseroId    INT NOT NULL CONSTRAINT FK_Pedidos_Usuarios REFERENCES dbo.Usuarios(UsuarioId),
    FechaHora   DATETIME2(0) NOT NULL CONSTRAINT DF_Pedidos_Fecha DEFAULT SYSDATETIME(),
    Estado      NVARCHAR(15) NOT NULL CONSTRAINT DF_Pedidos_Estado DEFAULT 'Pendiente'
                CONSTRAINT CK_Pedidos_Estado CHECK (Estado IN ('Pendiente','EnPreparacion','Listo','Entregado','Pagado','Cancelado')),
    Total       DECIMAL(12,2) NOT NULL CONSTRAINT DF_Pedidos_Total DEFAULT 0
);

CREATE TABLE dbo.DetallePedido (
    DetalleId      INT IDENTITY(1,1) CONSTRAINT PK_DetallePedido PRIMARY KEY,
    PedidoId       INT NOT NULL CONSTRAINT FK_Detalle_Pedidos REFERENCES dbo.Pedidos(PedidoId) ON DELETE CASCADE,
    PlatoId        INT NOT NULL CONSTRAINT FK_Detalle_Platos REFERENCES dbo.Platos(PlatoId),
    Cantidad       INT NOT NULL CONSTRAINT CK_Detalle_Cant CHECK (Cantidad > 0),
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    Subtotal       AS (Cantidad * PrecioUnitario) PERSISTED,
    Nota           NVARCHAR(150) NULL
);

/* ---------- Caja y pagos ---------- */
CREATE TABLE dbo.CierresCaja (
    CierreId      INT IDENTITY(1,1) CONSTRAINT PK_CierresCaja PRIMARY KEY,
    Fecha         DATE NOT NULL CONSTRAINT UQ_Cierres_Fecha UNIQUE,
    UsuarioId     INT NOT NULL CONSTRAINT FK_Cierres_Usuarios REFERENCES dbo.Usuarios(UsuarioId),
    BaseInicial   DECIMAL(12,2) NOT NULL,
    TotalVentas   DECIMAL(12,2) NOT NULL,
    TotalEfectivo DECIMAL(12,2) NOT NULL,
    TotalTarjeta  DECIMAL(12,2) NOT NULL,
    Observaciones NVARCHAR(200) NULL
);

CREATE TABLE dbo.Pagos (
    PagoId    INT IDENTITY(1,1) CONSTRAINT PK_Pagos PRIMARY KEY,
    PedidoId  INT NOT NULL CONSTRAINT FK_Pagos_Pedidos REFERENCES dbo.Pedidos(PedidoId),
    Metodo    NVARCHAR(15) NOT NULL CONSTRAINT CK_Pagos_Metodo CHECK (Metodo IN ('Efectivo','Tarjeta','Transferencia')),
    Monto     DECIMAL(12,2) NOT NULL CONSTRAINT CK_Pagos_Monto CHECK (Monto > 0),
    FechaHora DATETIME2(0) NOT NULL CONSTRAINT DF_Pagos_Fecha DEFAULT SYSDATETIME()
);

CREATE INDEX IX_Pedidos_Fecha   ON dbo.Pedidos(FechaHora) INCLUDE (Estado, Total);
CREATE INDEX IX_Pedidos_Estado  ON dbo.Pedidos(Estado);
CREATE INDEX IX_Reservas_Fecha  ON dbo.Reservas(FechaHora);
CREATE INDEX IX_Pagos_Fecha     ON dbo.Pagos(FechaHora);
GO

/* ---------- Vistas y procedimientos para reportes ---------- */
CREATE OR ALTER VIEW dbo.vw_VentasDiarias AS
SELECT CAST(p.FechaHora AS DATE) AS Fecha,
       COUNT(*)                  AS NumPedidos,
       SUM(p.Total)              AS TotalVentas
FROM dbo.Pedidos p
WHERE p.Estado = 'Pagado'
GROUP BY CAST(p.FechaHora AS DATE);
GO

CREATE OR ALTER VIEW dbo.vw_PlatosMasVendidos AS
SELECT pl.PlatoId, pl.Nombre, c.Nombre AS Categoria,
       SUM(d.Cantidad) AS UnidadesVendidas,
       SUM(d.Subtotal) AS Ingresos
FROM dbo.DetallePedido d
JOIN dbo.Pedidos p   ON p.PedidoId = d.PedidoId AND p.Estado = 'Pagado'
JOIN dbo.Platos pl   ON pl.PlatoId = d.PlatoId
JOIN dbo.Categorias c ON c.CategoriaId = pl.CategoriaId
GROUP BY pl.PlatoId, pl.Nombre, c.Nombre;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RecalcularTotalPedido @PedidoId INT AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Pedidos
    SET Total = ISNULL((SELECT SUM(Subtotal) FROM dbo.DetallePedido WHERE PedidoId = @PedidoId), 0)
    WHERE PedidoId = @PedidoId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CerrarCaja
    @Fecha DATE, @UsuarioId INT, @BaseInicial DECIMAL(12,2), @Observaciones NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.CierresCaja (Fecha, UsuarioId, BaseInicial, TotalVentas, TotalEfectivo, TotalTarjeta, Observaciones)
    SELECT @Fecha, @UsuarioId, @BaseInicial,
           ISNULL(SUM(Monto),0),
           ISNULL(SUM(CASE WHEN Metodo = 'Efectivo' THEN Monto END),0),
           ISNULL(SUM(CASE WHEN Metodo <> 'Efectivo' THEN Monto END),0),
           @Observaciones
    FROM dbo.Pagos
    WHERE CAST(FechaHora AS DATE) = @Fecha;
END
GO
