using Dapper;
using SIGR.Web.Data;
using SIGR.Web.Models;

namespace SIGR.Web.Services;

public class ReservaService(DbConnectionFactory db)
{
    /// <summary>Duración asumida de una reserva (para detectar choques de horario).</summary>
    public static readonly TimeSpan Duracion = TimeSpan.FromHours(2);

    private const string SelectReserva = """
        SELECT r.ReservaId, r.ClienteId, u.NombreCompleto AS Cliente, r.MesaId, m.Numero AS NumeroMesa,
               r.FechaHora, r.NumPersonas, r.Estado, r.Observaciones
        FROM dbo.Reservas r
        JOIN dbo.Usuarios u ON u.UsuarioId = r.ClienteId
        JOIN dbo.Mesas m ON m.MesaId = r.MesaId
        """;

    public async Task<List<Reserva>> ListarAsync(int? clienteId = null)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Reserva>(SelectReserva +
            (clienteId is null ? "" : " WHERE r.ClienteId=@clienteId") + " ORDER BY r.FechaHora DESC", new { clienteId })).ToList();
    }

    /// <summary>Mesas con capacidad suficiente y sin reserva activa que se cruce con el horario pedido.</summary>
    public async Task<List<Mesa>> MesasDisponiblesAsync(DateTime fechaHora, int personas)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Mesa>("""
            SELECT * FROM dbo.Mesas m
            WHERE m.Capacidad >= @personas
              AND NOT EXISTS (
                SELECT 1 FROM dbo.Reservas r
                WHERE r.MesaId = m.MesaId AND r.Estado IN ('Pendiente','Confirmada')
                  AND ABS(DATEDIFF(MINUTE, r.FechaHora, @fechaHora)) < @minutos)
            ORDER BY m.Capacidad, m.Numero
            """, new { fechaHora, personas, minutos = (int)Duracion.TotalMinutes })).ToList();
    }

    public async Task CrearAsync(int clienteId, int mesaId, DateTime fechaHora, int personas, string? obs)
    {
        if (fechaHora <= DateTime.Now) throw new ArgumentException("La reserva debe ser a futuro.");
        if (personas <= 0) throw new ArgumentException("Indica el número de personas.");
        using var cn = db.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        var libres = await cn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.Mesas m
            WHERE m.MesaId=@mesaId AND m.Capacidad >= @personas
              AND NOT EXISTS (SELECT 1 FROM dbo.Reservas r WITH (UPDLOCK, HOLDLOCK)
                WHERE r.MesaId=m.MesaId AND r.Estado IN ('Pendiente','Confirmada')
                  AND ABS(DATEDIFF(MINUTE, r.FechaHora, @fechaHora)) < @minutos)
            """, new { mesaId, personas, fechaHora, minutos = (int)Duracion.TotalMinutes }, tx);
        if (libres == 0) throw new InvalidOperationException("La mesa no está disponible en ese horario o no tiene capacidad suficiente.");
        await cn.ExecuteAsync("""
            INSERT dbo.Reservas (ClienteId, MesaId, FechaHora, NumPersonas, Observaciones)
            VALUES (@clienteId,@mesaId,@fechaHora,@personas,@obs)
            """, new { clienteId, mesaId, fechaHora, personas, obs }, tx);
        tx.Commit();
    }

    public async Task CambiarEstadoAsync(int reservaId, string estado, int? clienteId = null)
    {
        using var cn = db.Create();
        await cn.ExecuteAsync("""
            UPDATE dbo.Reservas SET Estado=@estado
            WHERE ReservaId=@reservaId AND Estado IN ('Pendiente','Confirmada')
              AND (@clienteId IS NULL OR ClienteId=@clienteId)
            """, new { reservaId, estado, clienteId });
    }
}
