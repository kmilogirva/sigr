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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    // Contraseña de prueba para los usuarios ficticios: Sigr2026!
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<UsuarioService>().SembrarPasswordsDemoAsync("Sigr2026!");
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

app.MapSigrApi();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
