using FluentAssertions;
using SIGR.Tests.Soporte;
using SIGR.Web.Models;
using SIGR.Web.Services;

namespace SIGR.Tests;

/// <summary>Cálculo de descuentos, propina y vigencia de cupones (módulo de pagos avanzados).</summary>
public class PagoServiceTests
{
    private static Cupon Cupon(string tipo, decimal valor) => new() { Tipo = tipo, Valor = valor };

    // ---------------- Descuento ----------------
    [Theory]
    [InlineData("Porcentaje", 10, 64000, 6400)]
    [InlineData("Porcentaje", 100, 64000, 64000)]
    [InlineData("Porcentaje", 0.5, 10000, 50)]
    [InlineData("Monto", 5000, 64000, 5000)]
    [InlineData("Monto", 5000, 3000, 3000)]      // el descuento nunca supera el subtotal
    [InlineData("Monto", 5000, 0, 0)]
    public void Descuento_se_calcula_y_nunca_supera_el_subtotal(string tipo, decimal valor, decimal subtotal, decimal esperado) =>
        PagoService.CalcularDescuento(Cupon(tipo, valor), subtotal).Should().Be(esperado);

    [Fact]
    public void Descuento_se_redondea_a_dos_decimales_alejandose_de_cero()
    {
        // 10 % de 12.345 = 1.234,5 exacto; 33,333 % de 100 = 33,333 -> 33,33
        PagoService.CalcularDescuento(Cupon("Porcentaje", 10), 12345m).Should().Be(1234.5m);
        PagoService.CalcularDescuento(Cupon("Porcentaje", 33.333m), 100m).Should().Be(33.33m);
    }

    [Fact]
    public void Descuento_con_valor_negativo_es_cero() =>
        PagoService.CalcularDescuento(Cupon("Monto", -100), 50000m).Should().Be(0);

    // ---------------- Propina ----------------
    [Theory]
    [InlineData(57600, 10, 5760)]
    [InlineData(57600, 0, 0)]
    [InlineData(100000, 15, 15000)]
    [InlineData(1, 5, 0.05)]
    [InlineData(-500, 10, 0)]      // base negativa
    [InlineData(10000, -10, 0)]    // porcentaje negativo
    public void Propina_sobre_la_base_neta(decimal baseNeta, decimal porcentaje, decimal esperada) =>
        PagoService.CalcularPropina(baseNeta, porcentaje).Should().Be(esperada);

    [Fact]
    public void Total_a_pagar_del_ejemplo_del_informe()
    {
        // 2 Bandeja paisa = 64.000, cupón BIENVENIDA10 (10 %) y propina sugerida del 10 % sobre lo neto
        var subtotal = 64000m;
        var descuento = PagoService.CalcularDescuento(Cupon("Porcentaje", 10), subtotal);
        var propina = PagoService.CalcularPropina(subtotal - descuento, 10);
        (subtotal - descuento + propina).Should().Be(63360m);
    }

    // ---------------- Vigencia de cupones ----------------
    private static readonly DateTime Hoy = new(2026, 11, 11);

    private static Cupon Vigente() => new()
    {
        Activo = true, VigenteDesde = Hoy.AddDays(-5), VigenteHasta = Hoy.AddDays(5), UsosMaximos = null, UsosActuales = 0
    };

    [Fact]
    public void Cupon_vigente_no_tiene_motivo_de_rechazo() =>
        PagoService.MotivoInvalido(Vigente(), Hoy).Should().BeNull();

    [Fact]
    public void Cupon_desactivado_es_rechazado()
    {
        var c = Vigente(); c.Activo = false;
        PagoService.MotivoInvalido(c, Hoy).Should().Contain("desactivado");
    }

    [Fact]
    public void Cupon_aun_no_vigente_es_rechazado()
    {
        var c = Vigente(); c.VigenteDesde = Hoy.AddDays(1);
        PagoService.MotivoInvalido(c, Hoy).Should().Contain("aún no está vigente");
    }

    [Fact]
    public void Cupon_vencido_es_rechazado()
    {
        var c = Vigente(); c.VigenteHasta = Hoy.AddDays(-1);
        PagoService.MotivoInvalido(c, Hoy).Should().Contain("vencido");
    }

    [Theory]
    [InlineData(0)]    // el mismo día de inicio
    [InlineData(10)]   // el mismo día de fin
    public void Los_limites_de_la_vigencia_son_inclusivos(int diasHastaFin)
    {
        var c = Vigente(); c.VigenteDesde = Hoy; c.VigenteHasta = Hoy.AddDays(diasHastaFin);
        PagoService.MotivoInvalido(c, Hoy).Should().BeNull();
    }

    [Theory]
    [InlineData(1, 1, false)]    // usos == máximo: agotado
    [InlineData(2, 1, false)]    // usos > máximo
    [InlineData(0, 1, true)]
    [InlineData(99, 100, true)]
    public void Cupon_con_limite_de_usos(int usados, int maximo, bool valido)
    {
        var c = Vigente(); c.UsosActuales = usados; c.UsosMaximos = maximo;
        (PagoService.MotivoInvalido(c, Hoy) is null).Should().Be(valido);
    }

    [Fact]
    public void Cupon_sin_maximo_de_usos_es_ilimitado()
    {
        var c = Vigente(); c.UsosActuales = 1_000_000; c.UsosMaximos = null;
        PagoService.MotivoInvalido(c, Hoy).Should().BeNull();
    }

    // ---------------- Validaciones al guardar un cupón (ocurren antes de tocar la base de datos) ----------------
    private static Cupon Nuevo(Action<Cupon>? cambio = null)
    {
        var c = new Cupon { Codigo = "PRUEBA10", Tipo = "Porcentaje", Valor = 10, VigenteDesde = Hoy, VigenteHasta = Hoy.AddDays(30) };
        cambio?.Invoke(c);
        return c;
    }

    [Theory]
    [InlineData("AB")]                      // muy corto
    [InlineData("CON ESPACIOS")]
    [InlineData("SIMBOLO#")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]   // 21 caracteres
    [InlineData("")]
    public async Task Guardar_cupon_rechaza_codigos_invalidos(string codigo)
    {
        var accion = () => Fabrica.Pagos().GuardarCuponAsync(Nuevo(c => c.Codigo = codigo));
        await accion.Should().ThrowAsync<ArgumentException>().WithMessage("*código*");
    }

    [Fact]
    public async Task Guardar_cupon_rechaza_tipo_desconocido() =>
        await ((Func<Task>)(() => Fabrica.Pagos().GuardarCuponAsync(Nuevo(c => c.Tipo = "Regalo"))))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*Tipo*");

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Guardar_cupon_rechaza_valor_no_positivo(decimal valor) =>
        await ((Func<Task>)(() => Fabrica.Pagos().GuardarCuponAsync(Nuevo(c => c.Valor = valor))))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*valor*");

    [Fact]
    public async Task Guardar_cupon_rechaza_porcentaje_mayor_a_100() =>
        await ((Func<Task>)(() => Fabrica.Pagos().GuardarCuponAsync(Nuevo(c => c.Valor = 100.01m))))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*100*");

    [Fact]
    public async Task Guardar_cupon_rechaza_fecha_final_anterior_a_la_inicial() =>
        await ((Func<Task>)(() => Fabrica.Pagos().GuardarCuponAsync(Nuevo(c => c.VigenteHasta = c.VigenteDesde.AddDays(-1)))))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*fecha*");

    [Fact]
    public async Task Guardar_cupon_rechaza_maximo_de_usos_no_positivo() =>
        await ((Func<Task>)(() => Fabrica.Pagos().GuardarCuponAsync(Nuevo(c => c.UsosMaximos = 0))))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*usos*");

    [Fact]
    public async Task Cobrar_rechaza_propina_negativa() =>
        await ((Func<Task>)(() => Fabrica.Pagos().CobrarAsync(1, null, -1, new List<PagoService.PagoParcial>())))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*propina*");
}
