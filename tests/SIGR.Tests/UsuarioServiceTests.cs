using Bogus;
using FluentAssertions;
using SIGR.Tests.Soporte;

namespace SIGR.Tests;

/// <summary>
/// Registro público y administración de usuarios. Las validaciones se ejecutan antes de abrir la base de datos,
/// por lo que no se necesita un SQL Server. Incluye las pruebas de regresión de H-01 y H-02.
/// </summary>
public class UsuarioServiceTests
{
    // ---------------- Registro de clientes ----------------
    [Theory]
    [InlineData("", "ana@correo.com", "abc123", "nombre es obligatorio")]          // H-02
    [InlineData("   ", "ana@correo.com", "abc123", "nombre es obligatorio")]       // H-02: solo espacios
    [InlineData("Ana", "sin-arroba", "abc123", "formato válido")]                  // H-01
    [InlineData("Ana", "a@b", "abc123", "formato válido")]                         // H-01: dominio sin punto
    [InlineData("Ana", "ana@correo.com", "abc12", "al menos 6")]                   // límite: 5 caracteres
    public async Task Registro_publico_rechaza_datos_invalidos(string nombre, string email, string clave, string fragmento)
    {
        var (ok, error) = await Fabrica.Usuarios().RegistrarClienteAsync(nombre, email, null, clave);
        ok.Should().BeFalse();
        error.Should().Contain(fragmento);
    }

    [Fact]
    public async Task Registro_publico_rechaza_telefono_de_mas_de_20_caracteres()
    {
        var (ok, error) = await Fabrica.Usuarios().RegistrarClienteAsync("Ana", "ana@correo.com", new string('3', 21), "abc123");
        ok.Should().BeFalse();
        error.Should().Contain("teléfono");
    }

    [Fact]
    public async Task Registro_publico_con_datos_aleatorios_invalidos_nunca_llega_a_la_base_de_datos()
    {
        var faker = new Faker("es");
        for (var i = 0; i < 20; i++)
        {
            var correoMalo = faker.Lorem.Word();   // una palabra sin @
            var (ok, _) = await Fabrica.Usuarios().RegistrarClienteAsync(faker.Name.FullName(), correoMalo, null, "abc123");
            ok.Should().BeFalse();
        }
    }

    // ---------------- Alta de personal (administrador) ----------------
    [Theory]
    [InlineData("", "ana@correo.com", "abc123", "nombre")]
    [InlineData("Ana", "correo-malo", "abc123", "correo")]
    [InlineData("Ana", "ana@correo.com", "abc12", "contraseña")]
    public async Task Crear_usuario_valida_en_el_servidor(string nombre, string email, string clave, string fragmento)
    {
        var accion = () => Fabrica.Usuarios().CrearAsync(nombre, email, null, "Mesero", clave);
        (await accion.Should().ThrowAsync<ArgumentException>()).Which.Message.Should().Contain(fragmento);
    }

    [Fact]
    public async Task Crear_usuario_rechaza_telefono_largo() =>
        await ((Func<Task>)(() => Fabrica.Usuarios().CrearAsync("Ana", "ana@correo.com", new string('1', 21), "Mesero", "abc123")))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*teléfono*");

    // ---------------- Reglas de protección ----------------
    [Fact]
    public async Task Un_administrador_no_puede_cambiar_su_propio_rol() =>
        await ((Func<Task>)(() => Fabrica.Usuarios().CambiarRolAsync(7, "Mesero", actorId: 7)))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*propio rol*");

    [Fact]
    public async Task Un_administrador_no_puede_desactivar_su_propia_cuenta() =>
        await ((Func<Task>)(() => Fabrica.Usuarios().CambiarActivoAsync(7, activo: false, actorId: 7)))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*propia cuenta*");

    [Fact]
    public async Task Restablecer_clave_exige_la_longitud_minima() =>
        await ((Func<Task>)(() => Fabrica.Usuarios().RestablecerClaveAsync(3, "12345")))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*6 caracteres*");

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public void El_servicio_se_construye_en_cualquier_entorno(string entorno) =>
        Fabrica.Usuarios(entorno).Should().NotBeNull();
}
