using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SIGR.Web.Components;
using SIGR.Web.Data;
using SIGR.Web.Endpoints;
using SIGR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/acceso-denegado";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        // Las rutas /api responden 401/403 en lugar de redirigir a la página de login.
        o.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        o.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<InventarioService>();
builder.Services.AddScoped<PagoService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<CajaService>();

var app = builder.Build();

// Cultura fija es-CO: moneda, números y fechas se ven igual en Windows, Linux y Docker
// (un contenedor Linux usa la cultura invariante y mostraría «¤4,500» en lugar de «$ 4.500»).
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("es-CO")
    .AddSupportedCultures("es-CO")
    .AddSupportedUICultures("es-CO"));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Contraseña de prueba para los usuarios ficticios: Sigr2026!
// Solo en Development o cuando se activa explícitamente (entornos de QA, p. ej. Docker): Demo__SembrarPasswords=true
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Demo:SembrarPasswords"))
{
    // Con reintentos: al arrancar junto a la base de datos (p. ej. en Docker) esta puede tardar unos segundos.
    // Si no se logra, no se detiene la app: el login repara las claves de demostración al vuelo.
    var log = app.Services.GetRequiredService<ILogger<Program>>();
    for (var intento = 1; intento <= 5; intento++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<UsuarioService>().SembrarPasswordsDemoAsync("Sigr2026!");
            break;
        }
        catch (Exception ex)
        {
            log.LogWarning("No se pudo sembrar la clave de demostración (intento {Intento}/5): {Mensaje}", intento, ex.Message);
            if (intento < 5) await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapPost("/account/login", async (HttpContext ctx, UsuarioService usuarios,
    [FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl) =>
{
    var u = await usuarios.ValidarAsync(email.Trim(), password);
    if (u is null) return Results.Redirect("/login?error=1");

    await ctx.SignInAsync(ApiEndpoints.CrearPrincipal(u));

    var destino = !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")
        ? returnUrl : "/";
    return Results.LocalRedirect(destino);
});

app.MapPost("/account/register", async (UsuarioService usuarios,
    [FromForm] string nombre, [FromForm] string email, [FromForm] string? telefono, [FromForm] string password) =>
{
    var (ok, error) = await usuarios.RegistrarClienteAsync(nombre.Trim(), email.Trim(), telefono?.Trim(), password);
    return ok ? Results.Redirect("/login?registrado=1")
              : Results.Redirect("/registro?error=" + Uri.EscapeDataString(error!));
});

app.MapPost("/account/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync();
    return Results.Redirect("/login");
});

// Comprobación de salud (usada por Docker): responde 200 si la base de datos contesta.
app.MapGet("/health", async (DbConnectionFactory db) =>
{
    try { using var cn = db.Create(); await cn.OpenAsync(); return Results.Ok(new { estado = "ok" }); }
    catch { return Results.Json(new { estado = "sin base de datos" }, statusCode: StatusCodes.Status503ServiceUnavailable); }
});

app.MapSigrApi();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
