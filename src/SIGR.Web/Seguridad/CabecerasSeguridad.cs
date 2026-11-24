namespace SIGR.Web.Seguridad;

/// <summary>
/// Cabeceras HTTP de seguridad que se agregan a todas las respuestas (hallazgos H-05 y H-06 de OWASP ZAP).
/// Las fuentes de Bootstrap Icons se sirven desde el propio sitio, por eso la política no necesita una CDN.
/// </summary>
public static class CabecerasSeguridad
{
    public const string PoliticaContenido =
        "default-src 'self'; " +
        "img-src 'self' data: https://images.unsplash.com; " +
        "font-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +      // Blazor y algunas vistas usan estilos en línea
        "script-src 'self' 'unsafe-inline'; " +     // el mapa de importación de Blazor es un script en línea
        "connect-src 'self'; " +                    // el circuito SignalR de Blazor Server usa el mismo origen
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    public static readonly IReadOnlyDictionary<string, string> Valores = new Dictionary<string, string>
    {
        ["Content-Security-Policy"] = PoliticaContenido,
        ["X-Content-Type-Options"] = "nosniff",
        ["X-Frame-Options"] = "DENY",
        ["Referrer-Policy"] = "strict-origin-when-cross-origin",
        ["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()",
        ["Cross-Origin-Opener-Policy"] = "same-origin",
        ["Cross-Origin-Resource-Policy"] = "same-origin",
        ["Cross-Origin-Embedder-Policy"] = "credentialless",   // permite las fotos externas sin enviar credenciales
    };

    public static IApplicationBuilder UseCabecerasSeguridad(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            contexto.Response.OnStarting(() =>
            {
                foreach (var (nombre, valor) in Valores)
                    contexto.Response.Headers[nombre] = valor;
                return Task.CompletedTask;
            });
            await siguiente();
        });
}
