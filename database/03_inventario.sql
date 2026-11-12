/* =====================================================================
   SIGR - Script 03: Módulo de inventario (insumos, recetas y kardex)
   Ejecutar después de 01_schema.sql y 02_seed.sql. Es re-ejecutable:
   no recrea ni borra nada, solo crea lo que falte.
   sqlcmd -S "localhost\SQLEXPRESS" -E -I -f 65001 -i database/03_inventario.sql
   ===================================================================== */
USE SIGR;
GO
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Insumos') IS NULL
CREATE TABLE dbo.Insumos (
    InsumoId      INT IDENTITY(1,1) CONSTRAINT PK_Insumos PRIMARY KEY,
    Nombre        NVARCHAR(60) NOT NULL CONSTRAINT UQ_Insumos_Nombre UNIQUE,
    Unidad        NVARCHAR(10) NOT NULL CONSTRAINT CK_Insumos_Unidad CHECK (Unidad IN ('kg','g','l','ml','und')),
    StockActual   DECIMAL(12,3) NOT NULL CONSTRAINT DF_Insumos_Stock DEFAULT 0 CONSTRAINT CK_Insumos_Stock CHECK (StockActual >= 0),
    StockMinimo   DECIMAL(12,3) NOT NULL CONSTRAINT DF_Insumos_Min DEFAULT 0 CONSTRAINT CK_Insumos_Min CHECK (StockMinimo >= 0),
    CostoUnitario DECIMAL(10,2) NOT NULL CONSTRAINT DF_Insumos_Costo DEFAULT 0 CONSTRAINT CK_Insumos_Costo CHECK (CostoUnitario >= 0),
    Activo        BIT NOT NULL CONSTRAINT DF_Insumos_Activo DEFAULT 1
);
GO

/* Receta: cantidad de insumo que consume UNA unidad de plato */
IF OBJECT_ID('dbo.Recetas') IS NULL
CREATE TABLE dbo.Recetas (
    PlatoId   INT NOT NULL CONSTRAINT FK_Recetas_Platos  REFERENCES dbo.Platos(PlatoId) ON DELETE CASCADE,
    InsumoId  INT NOT NULL CONSTRAINT FK_Recetas_Insumos REFERENCES dbo.Insumos(InsumoId),
    Cantidad  DECIMAL(12,3) NOT NULL CONSTRAINT CK_Recetas_Cant CHECK (Cantidad > 0),
    CONSTRAINT PK_Recetas PRIMARY KEY (PlatoId, InsumoId)
);
GO

/* Kardex: Cantidad con signo (positivo suma stock, negativo lo resta) */
IF OBJECT_ID('dbo.MovimientosInventario') IS NULL
CREATE TABLE dbo.MovimientosInventario (
    MovimientoId INT IDENTITY(1,1) CONSTRAINT PK_MovInv PRIMARY KEY,
    InsumoId     INT NOT NULL CONSTRAINT FK_MovInv_Insumos REFERENCES dbo.Insumos(InsumoId),
    Tipo         NVARCHAR(12) NOT NULL CONSTRAINT CK_MovInv_Tipo CHECK (Tipo IN ('Entrada','Consumo','Devolucion','Merma','Ajuste')),
    Cantidad     DECIMAL(12,3) NOT NULL CONSTRAINT CK_MovInv_Cant CHECK (Cantidad <> 0),
    FechaHora    DATETIME2(0) NOT NULL CONSTRAINT DF_MovInv_Fecha DEFAULT SYSDATETIME(),
    UsuarioId    INT NULL CONSTRAINT FK_MovInv_Usuarios REFERENCES dbo.Usuarios(UsuarioId),
    PedidoId     INT NULL CONSTRAINT FK_MovInv_Pedidos  REFERENCES dbo.Pedidos(PedidoId),
    Nota         NVARCHAR(150) NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MovInv_Insumo_Fecha')
    CREATE INDEX IX_MovInv_Insumo_Fecha ON dbo.MovimientosInventario(InsumoId, FechaHora DESC);
GO

/* ---------- Datos ficticios (solo si la tabla está vacía) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Insumos)
BEGIN
    INSERT dbo.Insumos (Nombre, Unidad, StockActual, StockMinimo, CostoUnitario) VALUES
    ('Arroz','kg',40,10,4200),               ('Fríjol','kg',25,8,7500),
    ('Carne de res','kg',30,10,32000),       ('Carne molida','kg',15,5,24000),
    ('Chicharrón','kg',12,4,22000),          ('Pollo pechuga','kg',25,8,16000),
    ('Huevo','und',200,60,600),              ('Plátano maduro','und',60,20,800),
    ('Papa criolla','kg',20,6,4500),         ('Harina de trigo','kg',30,10,3200),
    ('Queso mozzarella','kg',14,6,28000),    ('Queso parmesano','kg',1.2,1.5,58000),
    ('Queso crema','kg',7,3,24000),          ('Salsa de tomate','l',18,5,6500),
    ('Pepperoni','kg',3.5,3,36000),          ('Jamón','kg',7,3,21000),
    ('Piña','kg',8,3,5200),                  ('Pasta','kg',18,5,7200),
    ('Tocineta','kg',6,3,30000),             ('Crema de leche','l',10,4,12000),
    ('Chocolate','kg',8,3,26000),            ('Helado de vainilla','l',9,4,15000),
    ('Limón','kg',12,4,4800),                ('Mora','kg',9,4,9000),
    ('Leche','l',25,8,3800),                 ('Café molido','kg',2.4,2,38000),
    ('Gaseosa botella','und',80,24,2200),    ('Cerveza botella','und',90,24,4500),
    ('Lechuga','kg',8,3,5000),               ('Aguacate','und',40,12,2500),
    ('Totopos','kg',6,2,14000),              ('Frutos rojos','kg',5,2,32000);

    INSERT dbo.MovimientosInventario (InsumoId, Tipo, Cantidad, Nota)
    SELECT InsumoId, 'Entrada', StockActual, N'Inventario inicial' FROM dbo.Insumos WHERE StockActual > 0;

    INSERT dbo.Recetas (PlatoId, InsumoId, Cantidad)
    SELECT p.PlatoId, i.InsumoId, v.Cantidad
    FROM (VALUES
        ('Bandeja paisa','Arroz',0.15),('Bandeja paisa','Fríjol',0.12),('Bandeja paisa','Carne molida',0.10),
        ('Bandeja paisa','Chicharrón',0.08),('Bandeja paisa','Huevo',1),('Bandeja paisa','Plátano maduro',1),
        ('Churrasco 300 g','Carne de res',0.30),('Churrasco 300 g','Papa criolla',0.20),('Churrasco 300 g','Lechuga',0.05),
        ('Pechuga a la plancha','Pollo pechuga',0.25),('Pechuga a la plancha','Arroz',0.15),('Pechuga a la plancha','Lechuga',0.05),
        ('Pasta carbonara','Pasta',0.20),('Pasta carbonara','Tocineta',0.06),('Pasta carbonara','Huevo',1),('Pasta carbonara','Crema de leche',0.08),
        ('Pizza Margarita','Harina de trigo',0.25),('Pizza Margarita','Salsa de tomate',0.08),('Pizza Margarita','Queso mozzarella',0.20),
        ('Pizza Pepperoni','Harina de trigo',0.25),('Pizza Pepperoni','Salsa de tomate',0.08),('Pizza Pepperoni','Queso mozzarella',0.20),('Pizza Pepperoni','Pepperoni',0.10),
        ('Pizza Hawaiana','Harina de trigo',0.25),('Pizza Hawaiana','Salsa de tomate',0.08),('Pizza Hawaiana','Queso mozzarella',0.20),
        ('Pizza Hawaiana','Jamón',0.08),('Pizza Hawaiana','Piña',0.10),
        ('Brownie con helado','Chocolate',0.08),('Brownie con helado','Harina de trigo',0.05),('Brownie con helado','Huevo',1),('Brownie con helado','Helado de vainilla',0.10),
        ('Cheesecake de frutos rojos','Queso crema',0.12),('Cheesecake de frutos rojos','Frutos rojos',0.05),('Cheesecake de frutos rojos','Harina de trigo',0.03),
        ('Tres leches','Leche',0.20),('Tres leches','Huevo',1),('Tres leches','Crema de leche',0.05),
        ('Limonada natural','Limón',0.15),
        ('Jugo de mora en leche','Mora',0.12),('Jugo de mora en leche','Leche',0.20),
        ('Gaseosa','Gaseosa botella',1),('Cerveza artesanal','Cerveza botella',1),('Café americano','Café molido',0.02),
        ('Empanadas de carne (3 und)','Harina de trigo',0.10),('Empanadas de carne (3 und)','Carne molida',0.06),('Empanadas de carne (3 und)','Papa criolla',0.05),
        ('Nachos con guacamole','Totopos',0.15),('Nachos con guacamole','Aguacate',1),('Nachos con guacamole','Queso mozzarella',0.05),
        ('Ensalada César','Lechuga',0.15),('Ensalada César','Pollo pechuga',0.10),('Ensalada César','Queso parmesano',0.02),
        ('Crema de tomate','Salsa de tomate',0.20),('Crema de tomate','Crema de leche',0.05)
    ) v (Plato, Insumo, Cantidad)
    JOIN dbo.Platos  p ON p.Nombre = v.Plato
    JOIN dbo.Insumos i ON i.Nombre = v.Insumo;
END
GO
PRINT 'Inventario listo.';
