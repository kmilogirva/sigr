using Dapper;
using SIGR.Web.Data;
using SIGR.Web.Models;

namespace SIGR.Web.Services;

public class PedidoService(DbConnectionFactory db)
{
    private record PedidoInfo(int MesaId, string Estado, decimal Total);

    public static readonly string[] FlujoEstados = ["Pendiente", "EnPreparacion", "Listo", "Entregado"];

    private const string SelectPedido = """
        SELECT p.PedidoId, p.MesaId, m.Numero AS NumeroMesa, u.NombreCompleto AS Mesero,
               p.FechaHora, p.Estado, p.Total
        FROM dbo.Pedidos p
        JOIN dbo.Mesas m ON m.MesaId = p.MesaId
        JOIN dbo.Usuarios u ON u.UsuarioId = p.MeseroId
        """;

    public async Task<List<Mesa>> MesasAsync()
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Mesa>("SELECT * FROM dbo.Mesas ORDER BY Numero")).ToList();
    }

    public async Task<List<Pedido>> ActivosAsync()
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Pedido>(SelectPedido +
            " WHERE p.Estado NOT IN ('Pagado','Cancelado') ORDER BY p.FechaHora")).ToList();
    }

    public async Task<Pedido?> ObtenerAsync(int id)
    {
        using var cn = db.Create();
        return await cn.QuerySingleOrDefaultAsync<Pedido>(SelectPedido + " WHERE p.PedidoId=@id", new { id });
    }

    public async Task<List<DetallePedido>> DetalleAsync(int pedidoId)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<DetallePedido>("""
            SELECT d.DetalleId, d.PedidoId, d.PlatoId, pl.Nombre AS Plato, d.Cantidad,
                   d.PrecioUnitario, d.Subtotal, d.Nota
            FROM dbo.DetallePedido d JOIN dbo.Platos pl ON pl.PlatoId = d.PlatoId
            WHERE d.PedidoId=@pedidoId ORDER BY d.DetalleId
            """, new { pedidoId })).ToList();
    }

    public async Task<int> CrearAsync(int mesaId, int meseroId)
    {
        using var cn = db.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        var abierto = await cn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Pedidos WHERE MesaId=@mesaId AND Estado NOT IN ('Pagado','Cancelado')", new { mesaId }, tx);
        if (abierto > 0) throw new InvalidOperationException("La mesa ya tiene un pedido abierto.");
        var id = await cn.ExecuteScalarAsync<int>(
            "INSERT dbo.Pedidos (MesaId, MeseroId) OUTPUT INSERTED.PedidoId VALUES (@mesaId,@meseroId)", new { mesaId, meseroId }, tx);
        await cn.ExecuteAsync("UPDATE dbo.Mesas SET Estado='Ocupada' WHERE MesaId=@mesaId", new { mesaId }, tx);
        tx.Commit();
        return id;
    }

    public record ItemCarrito(int PlatoId, int Cantidad, string? Nota);

    /// <summary>
    /// Pedido autogestionado por el cliente: crea el pedido en una mesa libre y agrega todos los ítems
    /// en una sola transacción. El cliente queda como responsable (MeseroId) del pedido.
    /// </summary>
    public async Task<int> CrearPedidoClienteAsync(int clienteId, int mesaId, IReadOnlyCollection<ItemCarrito> items)
    {
        if (items.Count == 0) throw new ArgumentException("Agrega al menos un plato al pedido.");
        if (items.Any(i => i.Cantidad <= 0)) throw new ArgumentException("Las cantidades deben ser mayores a cero.");
        using var cn = db.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        var estadoMesa = await cn.ExecuteScalarAsync<string?>("SELECT Estado FROM dbo.Mesas WHERE MesaId=@mesaId", new { mesaId }, tx);
        if (estadoMesa != "Libre") throw new InvalidOperationException("La mesa seleccionada ya no está libre. Elige otra.");
        var abierto = await cn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Pedidos WHERE MesaId=@mesaId AND Estado NOT IN ('Pagado','Cancelado')", new { mesaId }, tx);
        if (abierto > 0) throw new InvalidOperationException("La mesa ya tiene un pedido abierto.");
        var id = await cn.ExecuteScalarAsync<int>(
            "INSERT dbo.Pedidos (MesaId, MeseroId) OUTPUT INSERTED.PedidoId VALUES (@mesaId,@clienteId)", new { mesaId, clienteId }, tx);
        foreach (var it in items)
        {
            var precio = await cn.ExecuteScalarAsync<decimal?>(
                "SELECT Precio FROM dbo.Platos WHERE PlatoId=@PlatoId AND Disponible=1", new { it.PlatoId }, tx)
                ?? throw new InvalidOperationException("Uno de los platos ya no está disponible.");
            await cn.ExecuteAsync(
                "INSERT dbo.DetallePedido (PedidoId,PlatoId,Cantidad,PrecioUnitario,Nota) VALUES (@id,@PlatoId,@Cantidad,@precio,@Nota)",
                new { id, it.PlatoId, it.Cantidad, precio, it.Nota }, tx);
        }
        await cn.ExecuteAsync("EXEC dbo.sp_RecalcularTotalPedido @id", new { id }, tx);
        await cn.ExecuteAsync("UPDATE dbo.Mesas SET Estado='Ocupada' WHERE MesaId=@mesaId", new { mesaId }, tx);
        tx.Commit();
        return id;
    }

    /// <summary>Pedidos hechos por un cliente, el más reciente primero.</summary>
    public async Task<List<Pedido>> DeClienteAsync(int clienteId)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Pedido>(SelectPedido + " WHERE p.MeseroId=@clienteId ORDER BY p.FechaHora DESC", new { clienteId })).ToList();
    }

    /// <summary>El cliente solo puede cancelar su propio pedido mientras siga Pendiente.</summary>
    public async Task CancelarDeClienteAsync(int pedidoId, int clienteId)
    {
        var p = await ObtenerAsync(pedidoId);
        if (p is null || !await EsDeClienteAsync(pedidoId, clienteId)) throw new InvalidOperationException("Pedido no encontrado.");
        if (p.Estado != "Pendiente") throw new InvalidOperationException("Solo puedes cancelar pedidos que aún no están en preparación.");
        await CancelarAsync(pedidoId);
    }

    public async Task<bool> EsDeClienteAsync(int pedidoId, int clienteId)
    {
        using var cn = db.Create();
        return await cn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Pedidos WHERE PedidoId=@pedidoId AND MeseroId=@clienteId", new { pedidoId, clienteId }) > 0;
    }

    public async Task AgregarItemAsync(int pedidoId, int platoId, int cantidad, string? nota)
    {
        if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero.");
        using var cn = db.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        var estado = await cn.ExecuteScalarAsync<string?>("SELECT Estado FROM dbo.Pedidos WHERE PedidoId=@pedidoId", new { pedidoId }, tx);
        if (estado is not ("Pendiente" or "EnPreparacion")) throw new InvalidOperationException("El pedido ya no admite cambios.");
        var precio = await cn.ExecuteScalarAsync<decimal?>("SELECT Precio FROM dbo.Platos WHERE PlatoId=@platoId AND Disponible=1", new { platoId }, tx)
            ?? throw new InvalidOperationException("El plato no está disponible.");
        await cn.ExecuteAsync("INSERT dbo.DetallePedido (PedidoId,PlatoId,Cantidad,PrecioUnitario,Nota) VALUES (@pedidoId,@platoId,@cantidad,@precio,@nota)",
            new { pedidoId, platoId, cantidad, precio, nota }, tx);
        await cn.ExecuteAsync("EXEC dbo.sp_RecalcularTotalPedido @pedidoId", new { pedidoId }, tx);
        tx.Commit();
    }

    public async Task QuitarItemAsync(int pedidoId, int detalleId)
    {
        using var cn = db.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        var estado = await cn.ExecuteScalarAsync<string?>("SELECT Estado FROM dbo.Pedidos WHERE PedidoId=@pedidoId", new { pedidoId }, tx);
        if (estado is not ("Pendiente" or "EnPreparacion")) throw new InvalidOperationException("El pedido ya no admite cambios.");
        await cn.ExecuteAsync("DELETE dbo.DetallePedido WHERE DetalleId=@detalleId AND PedidoId=@pedidoId", new { detalleId, pedidoId }, tx);
        await cn.ExecuteAsync("EXEC dbo.sp_RecalcularTotalPedido @pedidoId", new { pedidoId }, tx);
        tx.Commit();
    }

    /// <summary>Avanza al siguiente estado del flujo (Pendiente → EnPreparacion → Listo → Entregado).</summary>
    public async Task AvanzarAsync(int pedidoId)
    {
        using var cn = db.Create();
        var estado = await cn.ExecuteScalarAsync<string?>("SELECT Estado FROM dbo.Pedidos WHERE PedidoId=@pedidoId", new { pedidoId });
        var i = Array.IndexOf(FlujoEstados, estado);
        if (i < 0 || i >= FlujoEstados.Length - 1) return;
        await cn.ExecuteAsync("UPDATE dbo.Pedidos SET Estado=@e WHERE PedidoId=@pedidoId", new { e = FlujoEstados[i + 1], pedidoId });
    }

    public Task CancelarAsync(int pedidoId) => CerrarAsync(pedidoId, null);

    /// <summary>Cobra un pedido entregado: registra el pago, lo marca Pagado y libera la mesa.</summary>
    public Task PagarAsync(int pedidoId, string metodo) => CerrarAsync(pedidoId, metodo);

    private async Task CerrarAsync(int pedidoId, string? metodo)
    {
        using var cn = db.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        var p = await cn.QuerySingleOrDefaultAsync<PedidoInfo>(
            "SELECT MesaId, Estado, Total FROM dbo.Pedidos WHERE PedidoId=@pedidoId", new { pedidoId }, tx)
            ?? throw new InvalidOperationException("Pedido no encontrado.");
        if (p.Estado is "Pagado" or "Cancelado") throw new InvalidOperationException("El pedido ya está cerrado.");
        if (metodo is not null)
        {
            if (p.Estado != "Entregado") throw new InvalidOperationException("Solo se puede cobrar un pedido entregado.");
            if (p.Total <= 0) throw new InvalidOperationException("El pedido no tiene ítems.");
            await cn.ExecuteAsync("INSERT dbo.Pagos (PedidoId, Metodo, Monto) VALUES (@pedidoId,@metodo,@total)",
                new { pedidoId, metodo, total = p.Total }, tx);
        }
        await cn.ExecuteAsync("UPDATE dbo.Pedidos SET Estado=@e WHERE PedidoId=@pedidoId",
            new { e = metodo is null ? "Cancelado" : "Pagado", pedidoId }, tx);
        await cn.ExecuteAsync("UPDATE dbo.Mesas SET Estado='Libre' WHERE MesaId=@m", new { m = p.MesaId }, tx);
        tx.Commit();
    }
}
