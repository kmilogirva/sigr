using Dapper;
using SIGR.Web.Data;
using SIGR.Web.Models;

namespace SIGR.Web.Services;

public class CajaService(DbConnectionFactory db)
{
    public record ResumenDia(decimal TotalVentas, decimal Efectivo, decimal Otros, int NumPagos, int PedidosAbiertos);

    public async Task<ResumenDia> ResumenAsync(DateTime fecha)
    {
        using var cn = db.Create();
        var r = await cn.QuerySingleAsync<ResumenDia>("""
            SELECT ISNULL(SUM(Monto),0) AS TotalVentas,
                   ISNULL(SUM(CASE WHEN Metodo='Efectivo' THEN Monto END),0) AS Efectivo,
                   ISNULL(SUM(CASE WHEN Metodo<>'Efectivo' THEN Monto END),0) AS Otros,
                   COUNT(*) AS NumPagos,
                   (SELECT COUNT(*) FROM dbo.Pedidos WHERE CAST(FechaHora AS DATE)=@f AND Estado NOT IN ('Pagado','Cancelado')) AS PedidosAbiertos
            FROM dbo.Pagos WHERE CAST(FechaHora AS DATE)=@f
            """, new { f = fecha.Date });
        return r;
    }

    public async Task<CierreCaja?> CierreDelDiaAsync(DateTime fecha)
    {
        using var cn = db.Create();
        return await cn.QuerySingleOrDefaultAsync<CierreCaja>(CierreSelect + " WHERE c.Fecha=@f", new { f = fecha.Date });
    }

    public async Task<List<CierreCaja>> HistorialAsync()
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<CierreCaja>(CierreSelect + " ORDER BY c.Fecha DESC")).ToList();
    }

    private const string CierreSelect = """
        SELECT c.CierreId, c.Fecha, u.NombreCompleto AS Usuario, c.BaseInicial, c.TotalVentas,
               c.TotalEfectivo, c.TotalTarjeta, c.Observaciones
        FROM dbo.CierresCaja c JOIN dbo.Usuarios u ON u.UsuarioId = c.UsuarioId
        """;

    public async Task CerrarAsync(DateTime fecha, int usuarioId, decimal baseInicial, string? obs)
    {
        if (baseInicial < 0) throw new ArgumentException("La base inicial no puede ser negativa.");
        if (fecha.Date > DateTime.Today) throw new ArgumentException("No se puede cerrar una fecha futura.");
        var resumen = await ResumenAsync(fecha);
        if (resumen.PedidosAbiertos > 0)
            throw new InvalidOperationException($"Hay {resumen.PedidosAbiertos} pedido(s) abiertos ese día. Cóbralos o cancélalos antes de cerrar.");
        if (await CierreDelDiaAsync(fecha) is not null) throw new InvalidOperationException("Ese día ya tiene cierre de caja.");
        using var cn = db.Create();
        await cn.ExecuteAsync("EXEC dbo.sp_CerrarCaja @Fecha, @UsuarioId, @BaseInicial, @Observaciones",
            new { Fecha = fecha.Date, UsuarioId = usuarioId, BaseInicial = baseInicial, Observaciones = obs });
    }

    // ---------- Reportes ----------
    public async Task<List<VentaDiaria>> VentasDiariasAsync(int dias)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<VentaDiaria>(
            "SELECT * FROM dbo.vw_VentasDiarias WHERE Fecha >= DATEADD(DAY, -@dias, CAST(SYSDATETIME() AS DATE)) ORDER BY Fecha DESC",
            new { dias })).ToList();
    }

    public async Task<List<PlatoVendido>> TopPlatosAsync(int top = 10)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<PlatoVendido>(
            "SELECT TOP (@top) Nombre, Categoria, UnidadesVendidas, Ingresos FROM dbo.vw_PlatosMasVendidos ORDER BY UnidadesVendidas DESC, Ingresos DESC",
            new { top })).ToList();
    }
}
