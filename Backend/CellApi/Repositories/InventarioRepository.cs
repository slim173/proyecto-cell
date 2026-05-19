using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public class InventarioRepository : IInventarioRepository
{
    private readonly DbConnectionFactory _db;

    public InventarioRepository(DbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<InventarioMovimiento>> GetKardexAsync(
        int? productoId, DateTime? desde, DateTime? hasta)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT im.*,
                   p.nombre AS producto_nombre,
                   p.codigo AS producto_codigo
            FROM inventario_movimientos im
            JOIN productos p ON p.id = im.producto_id
            WHERE (@ProductoId IS NULL OR im.producto_id = @ProductoId)
              AND (@Desde IS NULL OR im.fecha >= @Desde)
              AND (@Hasta IS NULL OR im.fecha <= @Hasta)
            ORDER BY im.fecha DESC";

        return await conn.QueryAsync<InventarioMovimiento>(sql,
            new { ProductoId = productoId, Desde = desde, Hasta = hasta });
    }

    public async Task<int> CreateMovimientoAsync(InventarioMovimiento m)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            INSERT INTO inventario_movimientos
                (producto_id, tipo, cantidad, stock_anterior, stock_posterior,
                 referencia_tipo, referencia_id, observaciones, usuario)
            VALUES
                (@ProductoId, @Tipo, @Cantidad, @StockAnterior, @StockPosterior,
                 @ReferenciaTipo, @ReferenciaId, @Observaciones, @Usuario)
            RETURNING id";
        return await conn.ExecuteScalarAsync<int>(sql, m);
    }
}
