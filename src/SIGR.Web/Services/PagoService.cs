using System.Text.RegularExpressions;
using Dapper;
using SIGR.Web.Data;
using SIGR.Web.Models;

namespace SIGR.Web.Services;

/// <summary>Cobro de pedidos con cupón de descuento, propina y cuenta dividida entre varios métodos de pago.</summary>
public partial class PagoService(DbConnectionFactory db)
{
    public static readonly string[] Metodos = ["Efectivo", "Tarjeta", "Transferencia"];
    public record PagoParcial(string Metodo, decimal Monto);

    private record PedidoCobro(int MesaId, string Estado, decimal Total);

    [GeneratedRegex("^[A-Z0-9]{3,20}$")]
    private static partial Regex CodigoCuponRegex();

    // ---------------- Cálculos puros (sin base de datos) ----------------
    /// <summary>Valor del descuento de un cupón sobre un subtotal. Nunca supera el subtotal.</summary>
    public static decimal CalcularDescuento(Cupon c, decimal subtotal)
    {
        var d = c.Tipo == "Porcentaje" ? subtotal * c.Valor / 100m : c.Valor;
        return Math.Round(Math.Min(Math.Max(d, 0), subtotal), 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalcularPropina(decimal baseNeta, decimal porcentaje) =>
        Math.Round(Math.Max(baseNeta, 0) * Math.Max(porcentaje, 0) / 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>Devuelve el motivo por el que el cupón no se puede usar hoy, o null si es válido.</summary>
    public static string? MotivoInvalido(Cupon c, DateTime hoy)
    {
        if (!c.Activo) return "El cupón está desactivado.";
        if (hoy.Date < c.VigenteDesde.Date) return "El cupón aún no está vigente.";
        if (hoy.Date > c.VigenteHasta.Date) return "El cupón está vencido.";
        if (c.UsosMaximos is not null && c.UsosActuales >= c.UsosMaximos) return "El cupón ya alcanzó su límite de usos.";
        return null;
    }

    // ---------------- Cupones ----------------
    public async Task<List<Cupon>> CuponesAsync()
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Cupon>("SELECT * FROM dbo.Cupones ORDER BY Activo DESC, VigenteHasta DESC, Codigo")).ToList();
    }

    public async Task GuardarCuponAsync(Cupon c)
    {
        c.Codigo = (c.Codigo ?? "").Trim().ToUpperInvariant();
        if (!CodigoCuponRegex().IsMatch(c.Codigo)) throw new ArgumentException("El código debe tener entre 3 y 20 letras mayúsculas o números.");
        if (c.Tipo is not ("Porcentaje" or "Monto")) throw new ArgumentException("Tipo de cupón no válido.");
        if (c.Valor <= 0) throw new ArgumentException("El valor debe ser mayor a cero.");
        if (c.Tipo == "Porcentaje" && c.Valor > 100) throw new ArgumentException("Un porcentaje no puede superar 100.");
        if (c.VigenteHasta.Date < c.VigenteDesde.Date) throw new ArgumentException("La fecha final no puede ser anterior a la inicial.");
        if (c.UsosMaximos is <= 0) throw new ArgumentException("El máximo de usos debe ser mayor a cero (o vacío para ilimitado).");

        using var cn = db.Create();
        var repetido = await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Cupones WHERE Codigo=@Codigo AND CuponId<>@CuponId", c);
        if (repetido > 0) throw new InvalidOperationException("Ya existe un cupón con ese código.");
        if (c.CuponId == 0)
            await cn.ExecuteAsync("INSERT dbo.Cupones (Codigo,Tipo,Valor,VigenteDesde,VigenteHasta,UsosMaximos,Activo) VALUES (@Codigo,@Tipo,@Valor,@VigenteDesde,@VigenteHasta,@UsosMaximos,@Activo)", c);
        else
            await cn.ExecuteAsync("UPDATE dbo.Cupones SET Codigo=@Codigo, Tipo=@Tipo, Valor=@Valor, VigenteDesde=@VigenteDesde, VigenteHasta=@VigenteHasta, UsosMaximos=@UsosMaximos, Activo=@Activo WHERE CuponId=@CuponId", c);
    }

    /// <summary>Busca un cupón por código y verifica que se pueda usar. Lanza InvalidOperationException con el motivo.</summary>
    public async Task<Cupon> ValidarCuponAsync(string codigo)
    {
        codigo = (codigo ?? "").Trim().ToUpperInvariant();
        if (codigo.Length == 0) throw new InvalidOperationException("Escribe un código de cupón.");
        using var cn = db.Create();
        var c = await cn.QuerySingleOrDefaultAsync<Cupon>("SELECT * FROM dbo.Cupones WHERE Codigo=@codigo", new { codigo })
            ?? throw new InvalidOperationException("El cupón no existe.");
        var motivo = MotivoInvalido(c, DateTime.Today);
        if (motivo is not null) throw new InvalidOperationException(motivo);
        return c;
    }

    // ---------------- Cobro ----------------
    public async Task<List<Pago>> PagosDePedidoAsync(int pedidoId)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Pago>("SELECT * FROM dbo.Pagos WHERE PedidoId=@pedidoId ORDER BY PagoId", new { pedidoId })).ToList();
    }

    public async Task<string?> CodigoCuponDePedidoAsync(int pedidoId)
    {
        using var cn = db.Create();
        return await cn.ExecuteScalarAsync<string?>(
            "SELECT c.Codigo FROM dbo.Pedidos p JOIN dbo.Cupones c ON c.CuponId = p.CuponId WHERE p.PedidoId=@pedidoId", new { pedidoId });
    }

    /// <summary>
    /// Cobra un pedido Entregado en una sola transacción: aplica el cupón, suma la propina, registra uno o varios pagos
    /// (que deben sumar exactamente el total a pagar), marca el pedido como Pagado y libera la mesa.
    /// </summary>
    public async Task CobrarAsync(int pedidoId, string? codigoCupon, decimal propina, IReadOnlyList<PagoParcial> pagos)
    {
        if (propina < 0) throw new ArgumentException("La propina no puede ser negativa.");
        using var cn = db.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();

        var p = await cn.QuerySingleOrDefaultAsync<PedidoCobro>(
            "SELECT MesaId, Estado, Total FROM dbo.Pedidos WITH (UPDLOCK) WHERE PedidoId=@pedidoId", new { pedidoId }, tx)
            ?? throw new InvalidOperationException("Pedido no encontrado.");
        if (p.Estado != "Entregado") throw new InvalidOperationException("Solo se puede cobrar un pedido entregado.");
        if (p.Total <= 0) throw new InvalidOperationException("El pedido no tiene ítems.");

        decimal descuento = 0; int? cuponId = null;
        if (!string.IsNullOrWhiteSpace(codigoCupon))
        {
            var c = await cn.QuerySingleOrDefaultAsync<Cupon>("SELECT * FROM dbo.Cupones WITH (UPDLOCK) WHERE Codigo=@cod",
                new { cod = codigoCupon.Trim().ToUpperInvariant() }, tx) ?? throw new InvalidOperationException("El cupón no existe.");
            var motivo = MotivoInvalido(c, DateTime.Today);
            if (motivo is not null) throw new InvalidOperationException(motivo);
            descuento = CalcularDescuento(c, p.Total);
            cuponId = c.CuponId;
            await cn.ExecuteAsync("UPDATE dbo.Cupones SET UsosActuales = UsosActuales + 1 WHERE CuponId=@id", new { id = c.CuponId }, tx);
        }

        var neto = p.Total - descuento;
        if (propina > neto) throw new ArgumentException("La propina no puede superar el valor de la cuenta.");
        var totalAPagar = Math.Round(neto + propina, 2);

        if (pagos.Any(x => !Metodos.Contains(x.Metodo))) throw new ArgumentException("Método de pago no válido.");
        if (pagos.Any(x => x.Monto <= 0)) throw new ArgumentException("Cada pago debe ser mayor a cero.");
        var sumaPagos = Math.Round(pagos.Sum(x => x.Monto), 2);
        if (sumaPagos != totalAPagar)
            throw new InvalidOperationException($"Los pagos suman {sumaPagos:N0} y el total a pagar es {totalAPagar:N0}.");

        foreach (var x in pagos)
            await cn.ExecuteAsync("INSERT dbo.Pagos (PedidoId, Metodo, Monto) VALUES (@pedidoId,@Metodo,@Monto)", new { pedidoId, x.Metodo, x.Monto }, tx);
        await cn.ExecuteAsync("UPDATE dbo.Pedidos SET Estado='Pagado', Descuento=@descuento, Propina=@propina, CuponId=@cuponId WHERE PedidoId=@pedidoId",
            new { descuento, propina, cuponId, pedidoId }, tx);
        await cn.ExecuteAsync("UPDATE dbo.Mesas SET Estado='Libre' WHERE MesaId=@m", new { m = p.MesaId }, tx);
        tx.Commit();
    }
}
