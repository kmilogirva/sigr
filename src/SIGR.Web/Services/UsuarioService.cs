using Dapper;
using Microsoft.AspNetCore.Identity;
using SIGR.Web.Data;
using SIGR.Web.Models;

namespace SIGR.Web.Services;

public class UsuarioService(DbConnectionFactory db)
{
    private readonly PasswordHasher<Usuario> _hasher = new();

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
        var r = _hasher.VerifyHashedPassword(u, u.PasswordHash, password);
        return r == PasswordVerificationResult.Failed ? null : u;
    }

    /// <summary>Registro público: siempre crea un usuario con rol Cliente.</summary>
    public async Task<(bool Ok, string? Error)> RegistrarClienteAsync(string nombre, string email, string? telefono, string password)
    {
        if (password.Length < 6) return (false, "La contraseña debe tener al menos 6 caracteres.");
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
