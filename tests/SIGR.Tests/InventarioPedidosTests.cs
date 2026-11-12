using System.Security.Claims;
using Bogus;
using FluentAssertions;
using SIGR.Tests.Soporte;
using SIGR.Web.Endpoints;
using SIGR.Web.Models;
using SIGR.Web.Services;

namespace SIGR.Tests;

/// <summary>Validaciones del inventario y de los pedidos, y datos fijos del dominio.</summary>
public class InventarioPedidosTests
{
    // ---------------- Inventario: insumos ----------------
    private static Insumo Valido(Action<Insumo>? cambio = null)
    {
        var i = new Insumo { Nombre = "Harina", Unidad = "kg", StockActual = 5, StockMinimo = 1, CostoUnitario = 3200 };
        cambio?.Invoke(i);
        return i;
    }

    [Theory]
    [InlineData("", "kg", "nombre")]
    [InlineData("   ", "kg", "nombre")]
    [InlineData("Harina", "libras", "Unidad")]
    public async Task Guardar_insumo_rechaza_nombre_y_unidad_invalidos(string nombre, string unidad, string fragmento)
    {
        var accion = () => Fabrica.Inventario().GuardarInsumoAsync(Valido(i => { i.Nombre = nombre; i.Unidad = unidad; }), null);
        await accion.Should().ThrowAsync<ArgumentException>().WithMessage($"*{fragmento}*");
    }

    [Fact]
    public async Task Guardar_insumo_rechaza_nombre_de_mas_de_60_caracteres() =>
        await ((Func<Task>)(() => Fabrica.Inventario().GuardarInsumoAsync(Valido(i => i.Nombre = new string('x', 61)), null)))
            .Should().ThrowAsync<ArgumentException>();

    [Fact]
    public async Task Guardar_insumo_rechaza_stock_minimo_o_costo_negativo()
    {
        await ((Func<Task>)(() => Fabrica.Inventario().GuardarInsumoAsync(Valido(i => i.StockMinimo = -1), null)))
            .Should().ThrowAsync<ArgumentException>();
        await ((Func<Task>)(() => Fabrica.Inventario().GuardarInsumoAsync(Valido(i => i.CostoUnitario = -1), null)))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Un_insumo_nuevo_no_puede_empezar_con_stock_negativo() =>
        await ((Func<Task>)(() => Fabrica.Inventario().GuardarInsumoAsync(Valido(i => i.StockActual = -3), null)))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*stock inicial*");

    // ---------------- Inventario: movimientos manuales ----------------
    [Theory]
    [InlineData("Consumo", 1)]    // los consumos solo los genera el sistema, no el usuario
    [InlineData("Robo", 1)]
    [InlineData("Entrada", 0)]
    [InlineData("Entrada", -2)]   // una entrada debe ser positiva
    [InlineData("Merma", -2)]     // una merma se indica en positivo
    public async Task Movimiento_manual_invalido_es_rechazado(string tipo, decimal cantidad) =>
        await ((Func<Task>)(() => Fabrica.Inventario().RegistrarMovimientoAsync(1, tipo, cantidad, null, null)))
            .Should().ThrowAsync<ArgumentException>();

    [Fact]
    public void Las_unidades_y_tipos_permitidos_son_los_esperados()
    {
        InventarioService.Unidades.Should().BeEquivalentTo("kg", "g", "l", "ml", "und");
        InventarioService.TiposManuales.Should().BeEquivalentTo("Entrada", "Merma", "Ajuste");
    }

    [Fact]
    public void El_insumo_con_stock_igual_o_menor_al_minimo_esta_en_alerta()
    {
        new Insumo { StockActual = 2, StockMinimo = 3 }.StockBajo.Should().BeTrue();
        new Insumo { StockActual = 3, StockMinimo = 3 }.StockBajo.Should().BeTrue("el límite es inclusivo");
        new Insumo { StockActual = 3.001m, StockMinimo = 3 }.StockBajo.Should().BeFalse();
    }

    [Fact]
    public void El_costo_de_una_receta_suma_cantidad_por_costo_unitario()
    {
        var item = new RecetaItem { Cantidad = 0.25m, CostoUnitario = 3200 };
        item.Costo.Should().Be(800m);
    }

    // ---------------- Pedidos ----------------
    [Fact]
    public void El_flujo_de_estados_del_pedido_respeta_el_orden()
    {
        PedidoService.FlujoEstados.Should().Equal("Pendiente", "EnPreparacion", "Listo", "Entregado");
    }

    [Fact]
    public async Task Un_pedido_del_cliente_sin_items_es_rechazado() =>
        await ((Func<Task>)(() => Fabrica.Pedidos().CrearPedidoClienteAsync(5, 1, new List<PedidoService.ItemCarrito>())))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*al menos un plato*");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Un_pedido_del_cliente_con_cantidad_no_positiva_es_rechazado(int cantidad) =>
        await ((Func<Task>)(() => Fabrica.Pedidos().CrearPedidoClienteAsync(5, 1, new[] { new PedidoService.ItemCarrito(1, cantidad, null) })))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*cantidades*");

    [Fact]
    public async Task Agregar_un_item_con_cantidad_cero_es_rechazado() =>
        await ((Func<Task>)(() => Fabrica.Pedidos().AgregarItemAsync(1, 1, 0, null)))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*mayor a cero*");

    [Fact]
    public void El_total_a_pagar_es_subtotal_menos_descuento_mas_propina()
    {
        var p = new Pedido { Total = 64000, Descuento = 6400, Propina = 5760 };
        p.TotalAPagar.Should().Be(63360);
    }

    // ---------------- Identidad de la API ----------------
    [Fact]
    public void La_identidad_de_sesion_incluye_id_nombre_correo_y_rol()
    {
        var u = new Faker<Usuario>("es")
            .RuleFor(x => x.UsuarioId, f => f.Random.Int(1, 999))
            .RuleFor(x => x.NombreCompleto, f => f.Name.FullName())
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.Rol, "Administrador")
            .Generate();

        var principal = ApiEndpoints.CrearPrincipal(u);

        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(u.UsuarioId.ToString());
        principal.Identity!.Name.Should().Be(u.NombreCompleto);
        principal.FindFirst(ClaimTypes.Email)!.Value.Should().Be(u.Email);
        principal.IsInRole("Administrador").Should().BeTrue();
        principal.IsInRole("Cliente").Should().BeFalse();
        principal.Identity.IsAuthenticated.Should().BeTrue();
    }
}
