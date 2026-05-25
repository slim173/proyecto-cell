using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly DbConnectionFactory _db;

    public ClienteRepository(DbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Cliente>> GetAllAsync(bool soloActivos = true)
    {
        using var conn = _db.CreateConnection();
        var sql = soloActivos
            ? "SELECT * FROM clientes WHERE activo = true ORDER BY nombre, apellidos"
            : "SELECT * FROM clientes ORDER BY nombre, apellidos";
        return await conn.QueryAsync<Cliente>(sql);
    }

    public async Task<Cliente?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Cliente>(
            "SELECT * FROM clientes WHERE id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Cliente c)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            INSERT INTO clientes
                (nombre, apellidos, email, telefono, direccion, ciudad, codigo_postal, nif, activo, observaciones)
            VALUES
                (@Nombre, @Apellidos, @Email, @Telefono, @Direccion, @Ciudad, @CodigoPostal, @Nif, @Activo, @Observaciones)
            RETURNING id";
        return await conn.ExecuteScalarAsync<int>(sql, c);
    }

    public async Task UpdateAsync(Cliente c)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            UPDATE clientes SET
                nombre             = @Nombre,
                apellidos          = @Apellidos,
                email              = @Email,
                telefono           = @Telefono,
                direccion          = @Direccion,
                ciudad             = @Ciudad,
                codigo_postal      = @CodigoPostal,
                nif                = @Nif,
                activo             = @Activo,
                observaciones      = @Observaciones,
                fecha_modificacion = NOW()
            WHERE id = @Id";
        await conn.ExecuteAsync(sql, c);
    }

    public async Task<bool> EmailExistsAsync(string? email, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        using var conn = _db.CreateConnection();
        if (excludeId.HasValue)
            return await conn.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM clientes WHERE email = @Email AND id != @ExcludeId)",
                new { Email = email, ExcludeId = excludeId.Value });

        return await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM clientes WHERE email = @Email)",
            new { Email = email });
    }
}
