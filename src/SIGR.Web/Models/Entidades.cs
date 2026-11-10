namespace SIGR.Web.Models;

public class Usuario
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
    public string Rol { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telefono { get; set; }
    public string PasswordHash { get; set; } = "";
    public bool Activo { get; set; }
}

public class Categoria
{
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;
}

public class Plato
{
    public int PlatoId { get; set; }
    public int CategoriaId { get; set; }
    public string Categoria { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Disponible { get; set; } = true;
}

public class Mesa
{
    public int MesaId { get; set; }
    public int Numero { get; set; }
    public int Capacidad { get; set; }
    public string Estado { get; set; } = "Libre";
}

public class Pedido
{
    public int PedidoId { get; set; }
    public int MesaId { get; set; }
    public int NumeroMesa { get; set; }
    public string Mesero { get; set; } = "";
    public DateTime FechaHora { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public decimal Total { get; set; }
}

public class DetallePedido
{
    public int DetalleId { get; set; }
    public int PedidoId { get; set; }
    public int PlatoId { get; set; }
    public string Plato { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public string? Nota { get; set; }
}

public class Reserva
{
    public int ReservaId { get; set; }
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = "";
    public int MesaId { get; set; }
    public int NumeroMesa { get; set; }
    public DateTime FechaHora { get; set; }
    public int NumPersonas { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? Observaciones { get; set; }
}

public class VentaDiaria
{
    public DateTime Fecha { get; set; }
    public int NumPedidos { get; set; }
    public decimal TotalVentas { get; set; }
}

public class PlatoVendido
{
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public int UnidadesVendidas { get; set; }
    public decimal Ingresos { get; set; }
}

public class CierreCaja
{
    public int CierreId { get; set; }
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = "";
    public decimal BaseInicial { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjeta { get; set; }
    public string? Observaciones { get; set; }
}
