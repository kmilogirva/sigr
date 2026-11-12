using System.Net.Mail;

namespace SIGR.Web.Services;

/// <summary>Reglas de validación del lado del servidor (no dependen del navegador).</summary>
public static class Validaciones
{
    public const int ClaveMinima = 6;

    public static bool EsCorreoValido(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo) || correo.Length > 120) return false;
        if (!MailAddress.TryCreate(correo.Trim(), out var m)) return false;
        // Evita formas como "Nombre <a@b.c>" y exige un dominio con punto.
        return m.Address == correo.Trim() && m.Host.Contains('.') && !m.Host.EndsWith('.');
    }

    public static bool EsNombreValido(string? nombre) =>
        !string.IsNullOrWhiteSpace(nombre) && nombre.Trim().Length <= 100;

    public static bool EsClaveValida(string? clave) => clave is not null && clave.Length >= ClaveMinima;
}
