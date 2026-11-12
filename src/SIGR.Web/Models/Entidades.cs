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
    /// <summary>Subtotal de los ítems.</summary>
    public decimal Total { get; set; }
    public decimal Descuento { get; set; }
    public decimal Propina { get; set; }
    public decimal TotalAPagar => Total - Descuento + Propina;
    public DateTime? InicioPreparacion { get; set; }
    public DateTime? ListoEn { get; set; }
}

/// <summary>Pedido con sus ítems, tal como lo ve la cocina.</summary>
public class ComandaCocina
{
    public Pedido Pedido { get; set; } = new();
    public List<DetallePedido> Items { get; set; } = new();
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

public class Insumo
{
    public int InsumoId { get; set; }
    public string Nombre { get; set; } = "";
    public string Unidad { get; set; } = "kg";
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal CostoUnitario { get; set; }
    public bool Activo { get; set; } = true;
    public bool StockBajo => StockActual <= StockMinimo;
}

public class RecetaItem
{
    public int PlatoId { get; set; }
    public int InsumoId { get; set; }
    public string Insumo { get; set; } = "";
    public string Unidad { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Costo => Cantidad * CostoUnitario;
}

public class MovimientoInventario
{
    public int MovimientoId { get; set; }
    public int InsumoId { get; set; }
    public string Insumo { get; set; } = "";
    public string Unidad { get; set; } = "";
    public string Tipo { get; set; } = "";
    public decimal Cantidad { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Usuario { get; set; }
    public int? PedidoId { get; set; }
    public string? Nota { get; set; }
}

public class Cupon
{
    public int CuponId { get; set; }
    public string Codigo { get; set; } = "";
    public string Tipo { get; set; } = "Porcentaje";
    public decimal Valor { get; set; }
    public DateTime VigenteDesde { get; set; } = DateTime.Today;
    public DateTime VigenteHasta { get; set; } = DateTime.Today.AddMonths(1);
    public int? UsosMaximos { get; set; }
    public int UsosActuales { get; set; }
    public bool Activo { get; set; } = true;
}

public class Pago
{
    public int PagoId { get; set; }
    public int PedidoId { get; set; }
    public string Metodo { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime FechaHora { get; set; }
}
