/* =====================================================================
   SIGR - Script 04: Pagos avanzados (cupones, propina y descuento por pedido)
   Ejecutar después de 01_schema.sql y 02_seed.sql. Es re-ejecutable.
   sqlcmd -S "localhost\SQLEXPRESS" -E -I -f 65001 -i database/04_pagos_avanzados.sql
   Regla: Pedidos.Total es el subtotal de los ítems.
          Total a pagar = Total - Descuento + Propina
   ===================================================================== */
USE SIGR;
GO
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Cupones') IS NULL
CREATE TABLE dbo.Cupones (
    CuponId       INT IDENTITY(1,1) CONSTRAINT PK_Cupones PRIMARY KEY,
    Codigo        NVARCHAR(20) NOT NULL CONSTRAINT UQ_Cupones_Codigo UNIQUE,
    Tipo          NVARCHAR(12) NOT NULL CONSTRAINT CK_Cupones_Tipo CHECK (Tipo IN ('Porcentaje','Monto')),
    Valor         DECIMAL(10,2) NOT NULL CONSTRAINT CK_Cupones_Valor CHECK (Valor > 0),
    VigenteDesde  DATE NOT NULL,
    VigenteHasta  DATE NOT NULL,
    UsosMaximos   INT NULL CONSTRAINT CK_Cupones_UsosMax CHECK (UsosMaximos IS NULL OR UsosMaximos > 0),
    UsosActuales  INT NOT NULL CONSTRAINT DF_Cupones_Usos DEFAULT 0,
    Activo        BIT NOT NULL CONSTRAINT DF_Cupones_Activo DEFAULT 1,
    CONSTRAINT CK_Cupones_Fechas CHECK (VigenteHasta >= VigenteDesde),
    CONSTRAINT CK_Cupones_Pct CHECK (Tipo <> 'Porcentaje' OR Valor <= 100)
);
GO

IF COL_LENGTH('dbo.Pedidos', 'Descuento') IS NULL
    ALTER TABLE dbo.Pedidos ADD
        Descuento DECIMAL(12,2) NOT NULL CONSTRAINT DF_Pedidos_Descuento DEFAULT 0 CONSTRAINT CK_Pedidos_Descuento CHECK (Descuento >= 0),
        Propina   DECIMAL(12,2) NOT NULL CONSTRAINT DF_Pedidos_Propina   DEFAULT 0 CONSTRAINT CK_Pedidos_Propina   CHECK (Propina >= 0),
        CuponId   INT NULL CONSTRAINT FK_Pedidos_Cupones REFERENCES dbo.Cupones(CuponId);
GO

/* Las ventas se miden netas de descuento (la propina no es ingreso del restaurante) */
CREATE OR ALTER VIEW dbo.vw_VentasDiarias AS
SELECT CAST(p.FechaHora AS DATE) AS Fecha,
       COUNT(*)                  AS NumPedidos,
       SUM(p.Total - p.Descuento) AS TotalVentas
FROM dbo.Pedidos p
WHERE p.Estado = 'Pagado'
GROUP BY CAST(p.FechaHora AS DATE);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Cupones)
BEGIN
    DECLARE @hoy DATE = CAST(SYSDATETIME() AS DATE);
    INSERT dbo.Cupones (Codigo, Tipo, Valor, VigenteDesde, VigenteHasta, UsosMaximos, UsosActuales, Activo) VALUES
    ('BIENVENIDA10','Porcentaje',10,   DATEADD(DAY,-30,@hoy), DATEADD(DAY,365,@hoy), NULL, 0, 1),
    ('PROMO5000',   'Monto',     5000, DATEADD(DAY,-30,@hoy), DATEADD(DAY,365,@hoy), 100,  0, 1),
    ('NAVIDAD2025', 'Porcentaje',20,   '2025-12-01',          '2025-12-31',          NULL, 0, 1),   -- vencido
    ('UNICO',       'Monto',     3000, DATEADD(DAY,-30,@hoy), DATEADD(DAY,365,@hoy), 1,    1, 1);   -- agotado
END
GO
PRINT 'Pagos avanzados listo.';
