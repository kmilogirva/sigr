using Bogus;
using FluentAssertions;
using SIGR.Web.Services;

namespace SIGR.Tests;

/// <summary>Reglas de validación del servidor (corrigen los hallazgos H-01 y H-02).</summary>
public class ValidacionesTests
{
    [Theory]
    [InlineData("ana@correo.com")]
    [InlineData("nombre.apellido@dominio.com.co")]
    [InlineData("usuario+etiqueta@empresa.org")]
    [InlineData("  ana@correo.com  ")]   // los espacios de los extremos se toleran
    public void Correo_con_formato_valido_es_aceptado(string correo) =>
        Validaciones.EsCorreoValido(correo).Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]                          // dominio sin punto
    [InlineData("a@b.")]                         // dominio que termina en punto
    [InlineData("@dominio.com")]
    [InlineData("Nombre <ana@correo.com>")]      // forma con nombre visible
    [InlineData("dos@@correo.com")]
    public void Correo_con_formato_invalido_es_rechazado(string? correo) =>
        Validaciones.EsCorreoValido(correo).Should().BeFalse();

    [Fact]
    public void Correo_de_mas_de_120_caracteres_es_rechazado()
    {
        var largo = new string('a', 115) + "@b.co";   // 120 caracteres: límite
        var demasiado = new string('a', 116) + "@b.co";
        Validaciones.EsCorreoValido(largo).Should().BeTrue("120 es el máximo permitido por la columna");
        Validaciones.EsCorreoValido(demasiado).Should().BeFalse();
    }

    [Theory]
    [InlineData("Ana", true)]
    [InlineData("María Fernanda López", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Nombre_obligatorio(string? nombre, bool esperado) =>
        Validaciones.EsNombreValido(nombre).Should().Be(esperado);

    [Fact]
    public void Nombre_admite_hasta_100_caracteres()
    {
        Validaciones.EsNombreValido(new string('a', 100)).Should().BeTrue();
        Validaciones.EsNombreValido(new string('a', 101)).Should().BeFalse();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("12345", false)]     // un carácter menos del mínimo
    [InlineData("123456", true)]     // exactamente el mínimo
    [InlineData("Sigr2026!", true)]
    public void Clave_minima_de_seis_caracteres(string? clave, bool esperado) =>
        Validaciones.EsClaveValida(clave).Should().Be(esperado);

    [Fact]
    public void La_clave_minima_es_seis() => Validaciones.ClaveMinima.Should().Be(6);

    [Fact]
    public void Datos_generados_aleatoriamente_con_formato_correcto_son_aceptados()
    {
        var faker = new Faker("es");
        for (var i = 0; i < 50; i++)
        {
            Validaciones.EsCorreoValido(faker.Internet.Email()).Should().BeTrue();
            Validaciones.EsNombreValido(faker.Name.FullName()).Should().BeTrue();
            Validaciones.EsClaveValida(faker.Internet.Password(length: faker.Random.Int(6, 30))).Should().BeTrue();
        }
    }
}
