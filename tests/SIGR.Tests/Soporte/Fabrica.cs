using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SIGR.Web.Data;
using SIGR.Web.Services;

namespace SIGR.Tests.Soporte;

/// <summary>
/// Construye los servicios reales con dependencias falsas. La cadena de conexión es de relleno:
/// estas pruebas ejercitan solo las validaciones, que ocurren ANTES de abrir cualquier conexión.
/// </summary>
public static class Fabrica
{
    private static IConfiguration Configuracion(bool modoDemo = false) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SIGR"] = "Server=no-existe;Database=SIGR;User Id=x;Password=x;TrustServerCertificate=True;Connect Timeout=1",
            ["Demo:SembrarPasswords"] = modoDemo ? "true" : "false",
        }).Build();

    public static DbConnectionFactory Conexiones() => new(Configuracion());

    public static UsuarioService Usuarios(string entorno = "Production")
    {
        var host = Substitute.For<IHostEnvironment>();   // doble de prueba del entorno de ejecución
        host.EnvironmentName.Returns(entorno);
        return new UsuarioService(Conexiones(), Configuracion(), host);
    }

    public static PagoService Pagos() => new(Conexiones());
    public static PedidoService Pedidos() => new(Conexiones());
    public static InventarioService Inventario() => new(Conexiones());
}
