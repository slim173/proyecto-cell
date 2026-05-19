using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public class ConfiguracionRepository : IConfiguracionRepository
{
    private readonly DbConnectionFactory _db;

    public ConfiguracionRepository(DbConnectionFactory db) => _db = db;

    public async Task<string?> GetValorAsync(string clave)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT valor FROM configuracion WHERE clave = @Clave", new { Clave = clave });
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Configuracion>("SELECT * FROM configuracion");
        return rows
            .Where(r => r.Valor != null)
            .ToDictionary(r => r.Clave, r => r.Valor!);
    }

    public async Task SetValorAsync(string clave, string valor)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO configuracion (clave, valor)
            VALUES (@Clave, @Valor)
            ON CONFLICT (clave) DO UPDATE SET valor = EXCLUDED.valor",
            new { Clave = clave, Valor = valor });
    }

    public async Task SetMultipleAsync(Dictionary<string, string> valores)
    {
        using var conn = _db.CreateConnection();
        foreach (var (clave, valor) in valores)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO configuracion (clave, valor)
                VALUES (@Clave, @Valor)
                ON CONFLICT (clave) DO UPDATE SET valor = EXCLUDED.valor",
                new { Clave = clave, Valor = valor });
        }
    }
}
