using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SIGR.Web.Components;
using SIGR.Web.Data;
using SIGR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/acceso-denegado";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<PedidoService>();
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

    var identity = new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, u.UsuarioId.ToString()),
        new Claim(ClaimTypes.Name, u.NombreCompleto),
        new Claim(ClaimTypes.Email, u.Email),
        new Claim(ClaimTypes.Role, u.Rol),
    }, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(new ClaimsPrincipal(identity));

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

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
