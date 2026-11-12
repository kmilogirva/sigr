using Dapper;
using Microsoft.Data.SqlClient;
using SIGR.Web.Data;
using SIGR.Web.Models;

namespace SIGR.Web.Services;

/// <summary>Insumos, recetas y kardex. El consumo por pedido se invoca desde PedidoService dentro de su transacción.</summary>
public class InventarioService(DbConnectionFactory db)
{
    public static readonly string[] Unidades = ["kg", "g", "l", "ml", "und"];
    public static readonly string[] TiposManuales = ["Entrada", "Merma", "Ajuste"];

    private record FilaReceta(int InsumoId, string Nombre, decimal Cantidad);
    private record LineaPedido(int PlatoId, int Cantidad);

    // ---------------- Insumos ----------------
    public async Task<List<Insumo>> InsumosAsync(bool soloActivos = false)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Insumo>("SELECT * FROM dbo.Insumos" + (soloActivos ? " WHERE Activo=1" : "") + " ORDER BY Nombre")).ToList();
    }

    public async Task<int> ContarStockBajoAsync()
    {
        using var cn = db.Create();
        return await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Insumos WHERE Activo=1 AND StockActual <= StockMinimo");
    }

    public async Task GuardarInsumoAsync(Insumo i, int? usuarioId)
    {
        i.Nombre = (i.Nombre ?? "").Trim();
        if (i.Nombre.Length is 0 or > 60) throw new ArgumentException("El nombre es obligatorio (máximo 60 caracteres).");
        if (!Unidades.Contains(i.Unidad)) throw new ArgumentException("Unidad no válida.");
        if (i.StockMinimo < 0 || i.CostoUnitario < 0) throw new ArgumentException("El stock mínimo y el costo no pueden ser negativos.");
        if (i.InsumoId == 0 && i.StockActual < 0) throw new ArgumentException("El stock inicial no puede ser negativo.");

        using var cn = db.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        var repetido = await cn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Insumos WHERE Nombre=@Nombre AND InsumoId<>@InsumoId", i, tx);
        if (repetido > 0) throw new InvalidOperationException("Ya existe un insumo con ese nombre.");

        if (i.InsumoId == 0)
        {
            var id = await cn.ExecuteScalarAsync<int>("""
                INSERT dbo.Insumos (Nombre, Unidad, StockActual, StockMinimo, CostoUnitario, Activo)
                OUTPUT INSERTED.InsumoId VALUES (@Nombre,@Unidad,@StockActual,@StockMinimo,@CostoUnitario,@Activo)
                """, i, tx);
            if (i.StockActual > 0)
                await cn.ExecuteAsync("INSERT dbo.MovimientosInventario (InsumoId,Tipo,Cantidad,UsuarioId,Nota) VALUES (@id,'Entrada',@c,@usuarioId,N'Inventario inicial')",
                    new { id, c = i.StockActual, usuarioId }, tx);
        }
        else
        {
            await cn.ExecuteAsync("UPDATE dbo.Insumos SET Nombre=@Nombre, Unidad=@Unidad, StockMinimo=@StockMinimo, CostoUnitario=@CostoUnitario, Activo=@Activo WHERE InsumoId=@InsumoId", i, tx);
        }
        tx.Commit();
    }

    /// <summary>Movimiento manual: Entrada (compra), Merma (pérdida) o Ajuste (corrección con signo).</summary>
    public async Task RegistrarMovimientoAsync(int insumoId, string tipo, decimal cantidad, int? usuarioId, string? nota)
    {
        if (!TiposManuales.Contains(tipo)) throw new ArgumentException("Tipo de movimiento no válido.");
        if (cantidad == 0) throw new ArgumentException("La cantidad no puede ser cero.");
        if (tipo is "Entrada" or "Merma" && cantidad < 0) throw new ArgumentException($"En {tipo} la cantidad debe ser positiva.");
        var delta = tipo == "Merma" ? -cantidad : cantidad;

        using var cn = db.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        var n = await cn.ExecuteAsync("UPDATE dbo.Insumos SET StockActual = StockActual + @delta WHERE InsumoId=@insumoId AND StockActual + @delta >= 0",
            new { delta, insumoId }, tx);
        if (n == 0) throw new InvalidOperationException("El movimiento dejaría el stock en negativo (o el insumo no existe).");
        await cn.ExecuteAsync("INSERT dbo.MovimientosInventario (InsumoId,Tipo,Cantidad,UsuarioId,Nota) VALUES (@insumoId,@tipo,@delta,@usuarioId,@nota)",
            new { insumoId, tipo, delta, usuarioId, nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim() }, tx);
        tx.Commit();
    }

    public async Task<List<MovimientoInventario>> MovimientosAsync(int? insumoId = null, int top = 100)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<MovimientoInventario>("""
            SELECT TOP (@top) m.MovimientoId, m.InsumoId, i.Nombre AS Insumo, i.Unidad, m.Tipo, m.Cantidad, m.FechaHora,
                   u.NombreCompleto AS Usuario, m.PedidoId, m.Nota
            FROM dbo.MovimientosInventario m
            JOIN dbo.Insumos i ON i.InsumoId = m.InsumoId
            LEFT JOIN dbo.Usuarios u ON u.UsuarioId = m.UsuarioId
            WHERE (@insumoId IS NULL OR m.InsumoId = @insumoId)
            ORDER BY m.FechaHora DESC, m.MovimientoId DESC
            """, new { top, insumoId })).ToList();
    }

    // ---------------- Recetas ----------------
    public async Task<List<RecetaItem>> RecetaAsync(int platoId)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<RecetaItem>("""
            SELECT r.PlatoId, r.InsumoId, i.Nombre AS Insumo, i.Unidad, r.Cantidad, i.CostoUnitario
            FROM dbo.Recetas r JOIN dbo.Insumos i ON i.InsumoId = r.InsumoId
            WHERE r.PlatoId=@platoId ORDER BY i.Nombre
            """, new { platoId })).ToList();
    }

    public async Task GuardarRecetaItemAsync(int platoId, int insumoId, decimal cantidad)
    {
        if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero.");
        using var cn = db.Create();
        await cn.ExecuteAsync("""
            MERGE dbo.Recetas AS t USING (SELECT @platoId AS PlatoId, @insumoId AS InsumoId) AS s
            ON t.PlatoId = s.PlatoId AND t.InsumoId = s.InsumoId
            WHEN MATCHED THEN UPDATE SET Cantidad = @cantidad
            WHEN NOT MATCHED THEN INSERT (PlatoId, InsumoId, Cantidad) VALUES (@platoId, @insumoId, @cantidad);
            """, new { platoId, insumoId, cantidad });
    }

    public async Task QuitarRecetaItemAsync(int platoId, int insumoId)
    {
        using var cn = db.Create();
        await cn.ExecuteAsync("DELETE dbo.Recetas WHERE PlatoId=@platoId AND InsumoId=@insumoId", new { platoId, insumoId });
    }

    // ------------- Consumo (se ejecuta dentro de la transacción del pedido) -------------
    /// <summary>Descuenta los insumos de la receta × cantidad. Falla con InvalidOperationException si no hay stock suficiente.</summary>
    internal static async Task ConsumirAsync(SqlConnection cn, SqlTransaction tx, int pedidoId, int platoId, int cantidad)
    {
        var receta = await cn.QueryAsync<FilaReceta>(
            "SELECT r.InsumoId, i.Nombre, r.Cantidad FROM dbo.Recetas r JOIN dbo.Insumos i ON i.InsumoId=r.InsumoId WHERE r.PlatoId=@platoId",
            new { platoId }, tx);
        foreach (var f in receta)
        {
            var q = f.Cantidad * cantidad;
            var n = await cn.ExecuteAsync("UPDATE dbo.Insumos SET StockActual = StockActual - @q WHERE InsumoId=@id AND StockActual >= @q",
                new { q, id = f.InsumoId }, tx);
            if (n == 0) throw new InvalidOperationException($"Stock insuficiente de {f.Nombre} para preparar este plato.");
            await cn.ExecuteAsync("INSERT dbo.MovimientosInventario (InsumoId,Tipo,Cantidad,PedidoId) VALUES (@id,'Consumo',@neg,@pedidoId)",
                new { id = f.InsumoId, neg = -q, pedidoId }, tx);
        }
    }

    /// <summary>Reintegra al stock los insumos de una línea que se quita o se cancela.</summary>
    internal static async Task DevolverAsync(SqlConnection cn, SqlTransaction tx, int pedidoId, int platoId, int cantidad)
    {
        var receta = await cn.QueryAsync<FilaReceta>(
            "SELECT r.InsumoId, i.Nombre, r.Cantidad FROM dbo.Recetas r JOIN dbo.Insumos i ON i.InsumoId=r.InsumoId WHERE r.PlatoId=@platoId",
            new { platoId }, tx);
        foreach (var f in receta)
        {
            var q = f.Cantidad * cantidad;
            await cn.ExecuteAsync("UPDATE dbo.Insumos SET StockActual = StockActual + @q WHERE InsumoId=@id", new { q, id = f.InsumoId }, tx);
            await cn.ExecuteAsync("INSERT dbo.MovimientosInventario (InsumoId,Tipo,Cantidad,PedidoId,Nota) VALUES (@id,'Devolucion',@q,@pedidoId,N'Pedido cancelado o ítem retirado')",
                new { id = f.InsumoId, q, pedidoId }, tx);
        }
    }

    internal static async Task DevolverPedidoAsync(SqlConnection cn, SqlTransaction tx, int pedidoId)
    {
        var lineas = await cn.QueryAsync<LineaPedido>(
            "SELECT PlatoId, SUM(Cantidad) AS Cantidad FROM dbo.DetallePedido WHERE PedidoId=@pedidoId GROUP BY PlatoId", new { pedidoId }, tx);
        foreach (var l in lineas) await DevolverAsync(cn, tx, pedidoId, l.PlatoId, l.Cantidad);
    }
}
