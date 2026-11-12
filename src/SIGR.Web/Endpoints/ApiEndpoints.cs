using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SIGR.Web.Models;
using SIGR.Web.Services;

namespace SIGR.Web.Endpoints;

/// <summary>API REST de solo lectura (más login/logout por cookie) usada también para pruebas automatizadas.</summary>
public static class ApiEndpoints
{
    public record LoginRequest(string? Email, string? Password);
    public record PlatoDto(int PlatoId, string Nombre, string? Descripcion, string Categoria, decimal Precio);

    /// <summary>Identidad de cookie compartida por el login de formulario y el de la API.</summary>
    public static ClaimsPrincipal CrearPrincipal(Usuario u) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, u.UsuarioId.ToString()),
        new Claim(ClaimTypes.Name, u.NombreCompleto),
        new Claim(ClaimTypes.Email, u.Email),
        new Claim(ClaimTypes.Role, u.Rol),
    }, CookieAuthenticationDefaults.AuthenticationScheme));

    private static PlatoDto ADto(Plato p) => new(p.PlatoId, p.Nombre, p.Descripcion, p.Categoria, p.Precio);

    public static IEndpointRouteBuilder MapSigrApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        // ---------- Autenticación ----------
        api.MapPost("/auth/login", async (HttpContext ctx, UsuarioService usuarios, LoginRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrEmpty(req.Password))
                return Results.BadRequest(new { error = "Correo y contraseña son obligatorios." });

            var u = await usuarios.ValidarAsync(req.Email.Trim(), req.Password);
            if (u is null) return Results.Json(new { error = "Correo o contraseña incorrectos." }, statusCode: StatusCodes.Status401Unauthorized);

            await ctx.SignInAsync(CrearPrincipal(u));
            return Results.Ok(new { u.NombreCompleto, u.Rol });
        });

        api.MapPost("/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync();
            return Results.NoContent();
        });

        // ---------- Menú (público) ----------
        api.MapGet("/platos", async (MenuService menu, string? categoria) =>
        {
            var platos = await menu.PlatosAsync(soloDisponibles: true);
            if (!string.IsNullOrWhiteSpace(categoria))
                platos = platos.Where(p => p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase)).ToList();
            return Results.Ok(platos.Select(ADto));
        });

        api.MapGet("/platos/{id:int}", async (MenuService menu, int id) =>
        {
            var p = (await menu.PlatosAsync(soloDisponibles: true)).FirstOrDefault(x => x.PlatoId == id);
            return p is null ? Results.NotFound(new { error = "Plato no encontrado." }) : Results.Ok(ADto(p));
        });

        // ---------- Reportes (solo Administrador) ----------
        var admin = api.MapGroup("/reportes").RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Administrador" });

        admin.MapGet("/ventas", async (CajaService caja, int dias = 7) =>
        {
            if (dias is < 1 or > 365) return Results.BadRequest(new { error = "El parámetro 'dias' debe estar entre 1 y 365." });
            return Results.Ok(await caja.VentasDiariasAsync(dias));
        });

        admin.MapGet("/top-platos", async (CajaService caja, int top = 10) =>
        {
            if (top is < 1 or > 50) return Results.BadRequest(new { error = "El parámetro 'top' debe estar entre 1 y 50." });
            return Results.Ok(await caja.TopPlatosAsync(top));
        });

        return app;
    }
}
