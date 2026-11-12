using Dapper;
using Microsoft.AspNetCore.Identity;
using SIGR.Web.Data;
using SIGR.Web.Models;

namespace SIGR.Web.Services;

public class UsuarioService(DbConnectionFactory db, IConfiguration config, IHostEnvironment env)
{
    private const string ClaveDemo = "Sigr2026!";
    private const string PrefijoHashFicticio = "HASH_FICTICIO";
    private readonly PasswordHasher<Usuario> _hasher = new();

    /// <summary>Modo demostración/QA: Development o Demo:SembrarPasswords=true. Nunca debe estar activo en producción.</summary>
    private bool ModoDemo => env.IsDevelopment() || config.GetValue<bool>("Demo:SembrarPasswords");

    private const string SelectUsuario = """
        SELECT u.UsuarioId, u.RolId, r.Nombre AS Rol, u.NombreCompleto, u.Email,
               u.Telefono, u.PasswordHash, u.Activo
        FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId = u.RolId
        """;

    public async Task<Usuario?> ValidarAsync(string email, string password)
    {
        using var cn = db.Create();
        var u = await cn.QuerySingleOrDefaultAsync<Usuario>(
            SelectUsuario + " WHERE u.Email = @email AND u.Activo = 1", new { email });
        if (u is null) return null;

        // En modo demo la base puede haberse recreado con hashes ficticios: se reparan al vuelo.
        if (ModoDemo && u.PasswordHash.StartsWith(PrefijoHashFicticio, StringComparison.Ordinal))
        {
            await SembrarPasswordsDemoAsync(ClaveDemo);
            u = await cn.QuerySingleOrDefaultAsync<Usuario>(SelectUsuario + " WHERE u.Email = @email AND u.Activo = 1", new { email });
            if (u is null) return null;
        }

        // Un hash malformado en la base no debe provocar un error 500: simplemente no se autentica.
        PasswordVerificationResult r;
        try { r = _hasher.VerifyHashedPassword(u, u.PasswordHash, password); }
        catch (FormatException) { return null; }
        return r == PasswordVerificationResult.Failed ? null : u;
    }

    /// <summary>Registro público: siempre crea un usuario con rol Cliente.</summary>
    public async Task<(bool Ok, string? Error)> RegistrarClienteAsync(string nombre, string email, string? telefono, string password)
    {
        // Validación en el servidor (H-01, H-02): el navegador puede saltarse con facilidad.
        if (!Validaciones.EsNombreValido(nombre)) return (false, "El nombre es obligatorio (máximo 100 caracteres).");
        if (!Validaciones.EsCorreoValido(email)) return (false, "El correo no tiene un formato válido.");
        if (!Validaciones.EsClaveValida(password)) return (false, $"La contraseña debe tener al menos {Validaciones.ClaveMinima} caracteres.");
        if (telefono is { Length: > 20 }) return (false, "El teléfono admite máximo 20 caracteres.");
        nombre = nombre.Trim();
        email = email.Trim();
        telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();

        using var cn = db.Create();
        var existe = await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Usuarios WHERE Email=@email", new { email });
        if (existe > 0) return (false, "Ya existe un usuario con ese correo.");
        var hash = _hasher.HashPassword(new Usuario(), password);
        await cn.ExecuteAsync("""
            INSERT dbo.Usuarios (RolId, NombreCompleto, Email, Telefono, PasswordHash)
            VALUES ((SELECT RolId FROM dbo.Roles WHERE Nombre='Cliente'), @nombre, @email, @telefono, @hash)
            """, new { nombre, email, telefono, hash });
        return (true, null);
    }

    public async Task<IEnumerable<Usuario>> ListarPorRolAsync(string rol)
    {
        using var cn = db.Create();
        return await cn.QueryAsync<Usuario>(SelectUsuario + " WHERE r.Nombre=@rol AND u.Activo=1 ORDER BY u.NombreCompleto", new { rol });
    }

    // ---------------- Gestión de usuarios (Administrador) ----------------
    public async Task<List<string>> RolesAsync()
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<string>("SELECT Nombre FROM dbo.Roles ORDER BY RolId")).ToList();
    }

    public async Task<List<Usuario>> ListarAsync(string? rol = null, string? texto = null)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Usuario>(SelectUsuario + """
             WHERE (@rol IS NULL OR r.Nombre = @rol)
               AND (@texto IS NULL OR u.NombreCompleto LIKE @patron OR u.Email LIKE @patron)
             ORDER BY u.Activo DESC, r.RolId, u.NombreCompleto
            """, new { rol, texto, patron = $"%{texto}%" })).ToList();
    }

    /// <summary>Crea un usuario con cualquier rol (alta de personal). Valida todo en el servidor.</summary>
    public async Task CrearAsync(string nombre, string email, string? telefono, string rol, string password)
    {
        if (!Validaciones.EsNombreValido(nombre)) throw new ArgumentException("El nombre es obligatorio (máximo 100 caracteres).");
        if (!Validaciones.EsCorreoValido(email)) throw new ArgumentException("El correo no tiene un formato válido.");
        if (!Validaciones.EsClaveValida(password)) throw new ArgumentException($"La contraseña debe tener al menos {Validaciones.ClaveMinima} caracteres.");
        if (telefono is { Length: > 20 }) throw new ArgumentException("El teléfono admite máximo 20 caracteres.");

        using var cn = db.Create();
        var rolId = await cn.ExecuteScalarAsync<int?>("SELECT RolId FROM dbo.Roles WHERE Nombre=@rol", new { rol })
            ?? throw new ArgumentException("El rol indicado no existe.");
        email = email.Trim();
        if (await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Usuarios WHERE Email=@email", new { email }) > 0)
            throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        await cn.ExecuteAsync("""
            INSERT dbo.Usuarios (RolId, NombreCompleto, Email, Telefono, PasswordHash)
            VALUES (@rolId, @nombre, @email, @telefono, @hash)
            """, new { rolId, nombre = nombre.Trim(), email, telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim(), hash = _hasher.HashPassword(new Usuario(), password) });
    }

    private async Task<int> AdministradoresActivosAsync(Microsoft.Data.SqlClient.SqlConnection cn) =>
        await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId=u.RolId WHERE r.Nombre='Administrador' AND u.Activo=1");

    public async Task CambiarRolAsync(int usuarioId, string rol, int actorId)
    {
        if (usuarioId == actorId) throw new InvalidOperationException("No puedes cambiar tu propio rol.");
        using var cn = db.Create();
        var u = await cn.QuerySingleOrDefaultAsync<Usuario>(SelectUsuario + " WHERE u.UsuarioId=@usuarioId", new { usuarioId })
            ?? throw new InvalidOperationException("Usuario no encontrado.");
        var rolId = await cn.ExecuteScalarAsync<int?>("SELECT RolId FROM dbo.Roles WHERE Nombre=@rol", new { rol })
            ?? throw new ArgumentException("El rol indicado no existe.");
        if (u.Rol == "Administrador" && rol != "Administrador" && u.Activo && await AdministradoresActivosAsync(cn) <= 1)
            throw new InvalidOperationException("Debe quedar al menos un administrador activo.");
        await cn.ExecuteAsync("UPDATE dbo.Usuarios SET RolId=@rolId WHERE UsuarioId=@usuarioId", new { rolId, usuarioId });
    }

    public async Task CambiarActivoAsync(int usuarioId, bool activo, int actorId)
    {
        if (usuarioId == actorId && !activo) throw new InvalidOperationException("No puedes desactivar tu propia cuenta.");
        using var cn = db.Create();
        var u = await cn.QuerySingleOrDefaultAsync<Usuario>(SelectUsuario + " WHERE u.UsuarioId=@usuarioId", new { usuarioId })
            ?? throw new InvalidOperationException("Usuario no encontrado.");
        if (!activo && u.Rol == "Administrador" && u.Activo && await AdministradoresActivosAsync(cn) <= 1)
            throw new InvalidOperationException("Debe quedar al menos un administrador activo.");
        await cn.ExecuteAsync("UPDATE dbo.Usuarios SET Activo=@activo WHERE UsuarioId=@usuarioId", new { activo, usuarioId });
    }

    public async Task RestablecerClaveAsync(int usuarioId, string nuevaClave)
    {
        if (!Validaciones.EsClaveValida(nuevaClave)) throw new ArgumentException($"La contraseña debe tener al menos {Validaciones.ClaveMinima} caracteres.");
        using var cn = db.Create();
        var n = await cn.ExecuteAsync("UPDATE dbo.Usuarios SET PasswordHash=@hash WHERE UsuarioId=@usuarioId",
            new { hash = _hasher.HashPassword(new Usuario(), nuevaClave), usuarioId });
        if (n == 0) throw new InvalidOperationException("Usuario no encontrado.");
    }

    /// <summary>Solo desarrollo: reemplaza los hashes ficticios del seed por un hash real de la contraseña de prueba.</summary>
    public async Task SembrarPasswordsDemoAsync(string passwordDemo)
    {
        using var cn = db.Create();
        var pendientes = await cn.QueryAsync<Usuario>(
            SelectUsuario + " WHERE u.PasswordHash LIKE 'HASH_FICTICIO%'");
        foreach (var u in pendientes)
            await cn.ExecuteAsync("UPDATE dbo.Usuarios SET PasswordHash=@h WHERE UsuarioId=@id",
                new { h = _hasher.HashPassword(u, passwordDemo), id = u.UsuarioId });
    }
}
