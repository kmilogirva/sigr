/* =====================================================================
   SIGR - Script 02: Datos ficticios de prueba
   Ejecutar después de 01_schema.sql
   NOTA: PasswordHash es un valor FICTICIO. La app Blazor debe generar
   hashes reales (PasswordHasher<T>) al registrar/actualizar usuarios.
   ===================================================================== */
USE SIGR;
GO
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

INSERT dbo.Roles (Nombre) VALUES ('Administrador'), ('Mesero'), ('Cliente');

INSERT dbo.Usuarios (RolId, NombreCompleto, Email, Telefono, PasswordHash) VALUES
(1,'Laura Martínez Ríos','admin@sigr.test','3001110001','HASH_FICTICIO_ADMIN'),
(2,'Carlos Gómez Peña','carlos.mesero@sigr.test','3001110002','HASH_FICTICIO_MESERO1'),
(2,'Ana Torres Vélez','ana.mesera@sigr.test','3001110003','HASH_FICTICIO_MESERO2'),
(2,'Julián Rojas Díaz','julian.mesero@sigr.test','3001110004','HASH_FICTICIO_MESERO3'),
(3,'María Fernanda López','maria.lopez@correo.test','3105550001','HASH_FICTICIO_CLI1'),
(3,'Andrés Felipe Cardona','andres.cardona@correo.test','3105550002','HASH_FICTICIO_CLI2'),
(3,'Sofía Ramírez Cruz','sofia.ramirez@correo.test','3105550003','HASH_FICTICIO_CLI3'),
(3,'Daniel Herrera Soto','daniel.herrera@correo.test','3105550004','HASH_FICTICIO_CLI4'),
(3,'Valentina Ortiz Mejía','valentina.ortiz@correo.test','3105550005','HASH_FICTICIO_CLI5'),
(3,'Sebastián Aguirre León','sebastian.aguirre@correo.test','3105550006','HASH_FICTICIO_CLI6');

INSERT dbo.Categorias (Nombre, Descripcion) VALUES
('Entradas','Para comenzar'),
('Platos fuertes','Platos principales de la casa'),
('Pizzas','Horno de piedra'),
('Postres','Dulces para cerrar'),
('Bebidas','Frías y calientes');

INSERT dbo.Platos (CategoriaId, Nombre, Descripcion, Precio) VALUES
(1,'Empanadas de carne (3 und)','Con ají casero',9000),
(1,'Nachos con guacamole','Totopos crocantes y guacamole fresco',14000),
(1,'Ensalada César','Pollo, crutones y parmesano',16000),
(1,'Crema de tomate','Con albahaca y queso',12000),
(2,'Bandeja paisa','Fríjoles, arroz, chicharrón, carne molida, huevo y plátano',32000),
(2,'Churrasco 300 g','Con papas criollas y ensalada',38000),
(2,'Pechuga a la plancha','Con arroz y verduras salteadas',26000),
(2,'Salmón al horno','Con puré de papa y espárragos',45000),
(2,'Pasta carbonara','Tocineta, huevo y parmesano',27000),
(3,'Pizza Margarita','Tomate, mozzarella y albahaca',28000),
(3,'Pizza Pepperoni','Pepperoni y mozzarella',32000),
(3,'Pizza Hawaiana','Jamón y piña',30000),
(4,'Brownie con helado','Chocolate y helado de vainilla',13000),
(4,'Cheesecake de frutos rojos','Porción individual',15000),
(4,'Tres leches','Receta de la casa',12000),
(5,'Limonada natural','Jarra personal',7000),
(5,'Jugo de mora en leche','Vaso grande',8500),
(5,'Gaseosa','Botella 400 ml',5000),
(5,'Café americano','Grano local',4500),
(5,'Cerveza artesanal','Botella 330 ml',12000);
UPDATE dbo.Platos SET Disponible = 0 WHERE Nombre = 'Salmón al horno'; -- ejemplo de plato agotado

INSERT dbo.Mesas (Numero, Capacidad, Estado) VALUES
(1,2,'Libre'),(2,2,'Libre'),(3,4,'Ocupada'),(4,4,'Libre'),(5,4,'Reservada'),
(6,6,'Libre'),(7,6,'Libre'),(8,8,'Libre');

/* Reservas: relativas a hoy para que siempre haya datos vigentes */
DECLARE @hoy DATETIME2(0) = DATEADD(HOUR, 19, CAST(CAST(SYSDATETIME() AS DATE) AS DATETIME2(0)));
INSERT dbo.Reservas (ClienteId, MesaId, FechaHora, NumPersonas, Estado, Observaciones) VALUES
(5,5,@hoy,4,'Confirmada','Cumpleaños'),
(6,6,DATEADD(HOUR,1,@hoy),5,'Pendiente',NULL),
(7,1,DATEADD(DAY,1,@hoy),2,'Confirmada','Mesa junto a la ventana'),
(8,8,DATEADD(DAY,2,@hoy),8,'Pendiente','Reunión familiar'),
(9,4,DATEADD(DAY,-3,@hoy),3,'Completada',NULL),
(10,2,DATEADD(DAY,-1,@hoy),2,'Cancelada','Canceló el cliente');

/* Pedidos históricos (pagados) de los últimos 5 días + pedidos activos hoy */
DECLARE @d DATETIME2(0) = DATEADD(HOUR, 13, CAST(CAST(SYSDATETIME() AS DATE) AS DATETIME2(0)));
INSERT dbo.Pedidos (MesaId, MeseroId, FechaHora, Estado) VALUES
(1,2,DATEADD(DAY,-4,@d),'Pagado'),
(2,3,DATEADD(DAY,-4,DATEADD(HOUR,1,@d)),'Pagado'),
(4,2,DATEADD(DAY,-3,@d),'Pagado'),
(6,4,DATEADD(DAY,-3,DATEADD(HOUR,6,@d)),'Pagado'),
(7,3,DATEADD(DAY,-2,@d),'Pagado'),
(8,2,DATEADD(DAY,-2,DATEADD(HOUR,5,@d)),'Pagado'),
(1,4,DATEADD(DAY,-1,@d),'Pagado'),
(4,3,DATEADD(DAY,-1,DATEADD(HOUR,7,@d)),'Pagado'),
(3,2,SYSDATETIME(),'EnPreparacion'),
(6,3,SYSDATETIME(),'Pendiente'),
(2,4,SYSDATETIME(),'Listo');

INSERT dbo.DetallePedido (PedidoId, PlatoId, Cantidad, PrecioUnitario, Nota)
SELECT v.PedidoId, v.PlatoId, v.Cantidad, p.Precio, v.Nota
FROM (VALUES
 (1,5,2,NULL),(1,16,2,NULL),
 (2,10,1,'Sin albahaca'),(2,18,2,NULL),
 (3,6,2,'Término medio'),(3,20,2,NULL),(3,13,2,NULL),
 (4,1,2,NULL),(4,9,3,NULL),(4,16,3,NULL),(4,15,3,NULL),
 (5,11,2,NULL),(5,12,1,NULL),(5,17,3,NULL),
 (6,5,4,NULL),(6,2,2,NULL),(6,18,4,NULL),(6,14,4,NULL),
 (7,7,2,NULL),(7,19,2,NULL),
 (8,3,2,NULL),(8,6,1,NULL),(8,9,1,NULL),(8,20,2,NULL),
 (9,5,1,NULL),(9,16,1,NULL),
 (10,10,2,NULL),(10,17,2,NULL),(10,13,1,'Sin helado'),
 (11,7,1,NULL),(11,19,1,NULL)
) v(PedidoId, PlatoId, Cantidad, Nota)
JOIN dbo.Platos p ON p.PlatoId = v.PlatoId;

DECLARE @i INT = 1;
WHILE @i <= 11 BEGIN EXEC dbo.sp_RecalcularTotalPedido @i; SET @i += 1; END

/* Pagos de los pedidos 'Pagado' (alternando método) */
INSERT dbo.Pagos (PedidoId, Metodo, Monto, FechaHora)
SELECT PedidoId,
       CASE PedidoId % 3 WHEN 0 THEN 'Tarjeta' WHEN 1 THEN 'Efectivo' ELSE 'Transferencia' END,
       Total,
       DATEADD(MINUTE, 70, FechaHora)
FROM dbo.Pedidos WHERE Estado = 'Pagado';

/* Cierre de caja de un día anterior */
DECLARE @ayer DATE = DATEADD(DAY,-1,CAST(SYSDATETIME() AS DATE));
EXEC dbo.sp_CerrarCaja @Fecha=@ayer, @UsuarioId=1, @BaseInicial=200000, @Observaciones=N'Cierre sin novedades';
GO

/* ---------- Verificación rápida ---------- */
SELECT 'Usuarios' T, COUNT(*) N FROM dbo.Usuarios UNION ALL
SELECT 'Platos', COUNT(*) FROM dbo.Platos UNION ALL
SELECT 'Pedidos', COUNT(*) FROM dbo.Pedidos UNION ALL
SELECT 'Pagos', COUNT(*) FROM dbo.Pagos UNION ALL
SELECT 'Reservas', COUNT(*) FROM dbo.Reservas;
SELECT * FROM dbo.vw_VentasDiarias ORDER BY Fecha;
SELECT TOP 5 * FROM dbo.vw_PlatosMasVendidos ORDER BY UnidadesVendidas DESC;
GO
