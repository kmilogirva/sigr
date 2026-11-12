/* =====================================================================
   SIGR - Script 05: Módulo de cocina (rol Cocinero y tiempos de preparación)
   Ejecutar después de 01_schema.sql y 02_seed.sql. Es re-ejecutable.
   sqlcmd -S "localhost\SQLEXPRESS" -E -I -f 65001 -i database/05_cocina.sql
   ===================================================================== */
USE SIGR;
GO
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Cocinero')
    INSERT dbo.Roles (Nombre) VALUES ('Cocinero');
GO

/* Marcas de tiempo para medir cuánto tarda la cocina */
IF COL_LENGTH('dbo.Pedidos', 'InicioPreparacion') IS NULL
    ALTER TABLE dbo.Pedidos ADD InicioPreparacion DATETIME2(0) NULL, ListoEn DATETIME2(0) NULL;
GO

/* Usuario ficticio de cocina (la app, en Development, reemplaza el hash por el de la clave de prueba) */
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = 'cocina@sigr.test')
    INSERT dbo.Usuarios (RolId, NombreCompleto, Email, Telefono, PasswordHash)
    VALUES ((SELECT RolId FROM dbo.Roles WHERE Nombre = 'Cocinero'), N'Marta Salazar Ruiz', 'cocina@sigr.test', '3001110005', 'HASH_FICTICIO_COCINA');
GO
PRINT 'Cocina lista.';
