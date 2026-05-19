using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public interface IGarantiaRepository
{
    Task<IEnumerable<Garantia>> GetAllAsync(int? clienteId = null);
    Task<Garantia?> GetByIdAsync(int id);
    Task<int> CreateAsync(Garantia g);
    Task UpdateEstadoAsync(int id, string estado);
}

public class GarantiaRepository : IGarantiaRepository
{
    private readonly DbConnectionFactory _db;
    public GarantiaRepository(DbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Garantia>> GetAllAsync(int? clienteId = null)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT g.*, c.nombre AS cliente_nombre, c.apellidos AS cliente_apellidos,
                   c.telefono AS cliente_telefono
            FROM garantias g
            JOIN clientes c ON c.id = g.cliente_id
            WHERE (@ClienteId IS NULL OR g.cliente_id = @ClienteId)
            ORDER BY g.fecha_fin ASC";
        return await conn.QueryAsync<Garantia>(sql, new { ClienteId = clienteId });
    }

    public async Task<Garantia?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Garantia>(@"
            SELECT g.*, c.nombre AS cliente_nombre, c.apellidos AS cliente_apellidos,
                   c.telefono AS cliente_telefono
            FROM garantias g
            JOIN clientes c ON c.id = g.cliente_id
            WHERE g.id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Garantia g)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO garantias
                (numero_garantia, tipo, referencia_id, cliente_id, producto_descripcion,
                 fecha_inicio, fecha_fin, meses, estado, observaciones)
            VALUES
                (@NumeroGarantia, @Tipo, @ReferenciaId, @ClienteId, @ProductoDescripcion,
                 @FechaInicio, @FechaFin, @Meses, @Estado, @Observaciones)
            RETURNING id", g);
    }

    public async Task UpdateEstadoAsync(int id, string estado)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE garantias SET estado = @Estado WHERE id = @Id",
            new { Id = id, Estado = estado });
    }
}
