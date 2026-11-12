using FluentAssertions;
using SIGR.Web.Seguridad;
using SIGR.Web.Services;

namespace SIGR.Tests;

/// <summary>Correcciones de seguridad: rutas de retorno seguras (H-07) y cabeceras HTTP (H-05, H-06).</summary>
public class SeguridadTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/reportes")]
    [InlineData("/mis-pedidos?nuevo=1")]
    [InlineData("/admin/inventario")]
    public void Ruta_local_es_aceptada(string ruta) =>
        Validaciones.EsRutaLocalSegura(ruta).Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("reportes")]                       // no empieza con «/»
    [InlineData("//sitio-malicioso.com")]          // URL relativa al protocolo: cambia de sitio
    [InlineData("/\\sitio-malicioso.com")]         // el navegador trata «\» como «/»
    [InlineData("http://sitio-malicioso.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/ruta\r\nSet-Cookie: x=1")]       // inyección de cabeceras
    public void Ruta_no_local_o_peligrosa_es_rechazada(string? ruta) =>
        Validaciones.EsRutaLocalSegura(ruta).Should().BeFalse();

    [Fact]
    public void Ruta_demasiado_larga_es_rechazada() =>
        Validaciones.EsRutaLocalSegura("/" + new string('a', 2000)).Should().BeFalse();

    [Theory]
    [InlineData("Content-Security-Policy")]
    [InlineData("X-Content-Type-Options")]
    [InlineData("X-Frame-Options")]
    [InlineData("Referrer-Policy")]
    [InlineData("Permissions-Policy")]
    [InlineData("Cross-Origin-Opener-Policy")]
    [InlineData("Cross-Origin-Resource-Policy")]
    [InlineData("Cross-Origin-Embedder-Policy")]
    public void Se_definen_todas_las_cabeceras_de_seguridad(string cabecera) =>
        CabecerasSeguridad.Valores.Should().ContainKey(cabecera);

    [Fact]
    public void X_Content_Type_Options_evita_el_sniffing() =>
        CabecerasSeguridad.Valores["X-Content-Type-Options"].Should().Be("nosniff");

    [Fact]
    public void La_politica_de_contenido_no_usa_comodines_y_define_los_directivos_sin_respaldo()
    {
        var csp = CabecerasSeguridad.PoliticaContenido;
        csp.Should().NotContain("*", "un comodín permite cargar recursos de cualquier origen");
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("frame-ancestors 'none'");   // anti-clickjacking
        csp.Should().Contain("object-src 'none'");
        csp.Should().Contain("base-uri 'self'");
        csp.Should().Contain("form-action 'self'");
    }

    [Fact]
    public void La_politica_no_permite_scripts_ni_fuentes_de_terceros()
    {
        var csp = CabecerasSeguridad.PoliticaContenido;
        csp.Should().Contain("script-src 'self'");
        csp.Should().NotContain("cdn.", "los recursos (iconos) se sirven desde el propio sitio: hallazgo H-06");
        csp.Should().Contain("font-src 'self'");
    }
}
