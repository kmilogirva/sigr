using Microsoft.Data.SqlClient;

namespace SIGR.Web.Data;

/// <summary>Crea conexiones a SQL Server usando la cadena "SIGR" de la configuración.</summary>
public class DbConnectionFactory(IConfiguration config)
{
    private readonly string _cs = config.GetConnectionString("SIGR")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SIGR");

    public SqlConnection Create() => new(_cs);
}
