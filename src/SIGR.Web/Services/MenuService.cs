using Dapper;
using SIGR.Web.Data;
using SIGR.Web.Models;

namespace SIGR.Web.Services;

public class MenuService(DbConnectionFactory db)
{
    // ---------- Categorías ----------
    public async Task<List<Categoria>> CategoriasAsync(bool soloActivas = false)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Categoria>(
            "SELECT * FROM dbo.Categorias" + (soloActivas ? " WHERE Activa=1" : "") + " ORDER BY Nombre")).ToList();
    }

    public async Task GuardarCategoriaAsync(Categoria c)
    {
        using var cn = db.Create();
        if (c.CategoriaId == 0)
            await cn.ExecuteAsync("INSERT dbo.Categorias (Nombre, Descripcion, Activa) VALUES (@Nombre,@Descripcion,@Activa)", c);
        else
            await cn.ExecuteAsync("UPDATE dbo.Categorias SET Nombre=@Nombre, Descripcion=@Descripcion, Activa=@Activa WHERE CategoriaId=@CategoriaId", c);
    }

    /// <summary>Devuelve false si la categoría tiene platos asociados.</summary>
    public async Task<bool> EliminarCategoriaAsync(int id)
    {
        using var cn = db.Create();
        var n = await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Platos WHERE CategoriaId=@id", new { id });
        if (n > 0) return false;
        await cn.ExecuteAsync("DELETE dbo.Categorias WHERE CategoriaId=@id", new { id });
        return true;
    }

    // ---------- Platos ----------
    private const string SelectPlato = """
        SELECT p.PlatoId, p.CategoriaId, c.Nombre AS Categoria, p.Nombre, p.Descripcion, p.Precio, p.Disponible
        FROM dbo.Platos p JOIN dbo.Categorias c ON c.CategoriaId = p.CategoriaId
        """;

    public async Task<List<Plato>> PlatosAsync(bool soloDisponibles = false)
    {
        using var cn = db.Create();
        return (await cn.QueryAsync<Plato>(SelectPlato +
            (soloDisponibles ? " WHERE p.Disponible=1 AND c.Activa=1" : "") + " ORDER BY c.Nombre, p.Nombre")).ToList();
    }

    public async Task GuardarPlatoAsync(Plato p)
    {
        using var cn = db.Create();
        if (p.PlatoId == 0)
            await cn.ExecuteAsync("INSERT dbo.Platos (CategoriaId,Nombre,Descripcion,Precio,Disponible) VALUES (@CategoriaId,@Nombre,@Descripcion,@Precio,@Disponible)", p);
        else
            await cn.ExecuteAsync("UPDATE dbo.Platos SET CategoriaId=@CategoriaId, Nombre=@Nombre, Descripcion=@Descripcion, Precio=@Precio, Disponible=@Disponible WHERE PlatoId=@PlatoId", p);
    }

    /// <summary>Si el plato ya fue pedido no se borra: se marca como no disponible.</summary>
    public async Task<string> EliminarPlatoAsync(int id)
    {
        using var cn = db.Create();
        var usado = await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.DetallePedido WHERE PlatoId=@id", new { id });
        if (usado > 0)
        {
            await cn.ExecuteAsync("UPDATE dbo.Platos SET Disponible=0 WHERE PlatoId=@id", new { id });
            return "El plato tiene pedidos asociados: se marcó como no disponible.";
        }
        await cn.ExecuteAsync("DELETE dbo.Platos WHERE PlatoId=@id", new { id });
        return "Plato eliminado.";
    }
}
