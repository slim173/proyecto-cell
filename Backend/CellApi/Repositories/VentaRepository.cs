using CellApi.Data;
using CellApi.Models;
using Dapper;
using System.Data;

namespace CellApi.Repositories;

public class VentaRepository : IVentaRepository
{
    private readonly DbConnectionFactory _db;

    public VentaRepository(DbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Venta>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT v.*,
                   c.nombre          AS cliente_nombre,
                   c.apellidos       AS cliente_apellidos,
                   c.email           AS cliente_email,
                   c.nif             AS cliente_nif,
                   c.telefono        AS cliente_telefono,
                   f.numero_factura
            FROM ventas v
            JOIN clientes c ON c.id = v.cliente_id
            LEFT JOIN facturas f ON f.venta_id = v.id
            ORDER BY v.fecha DESC";
        return await conn.QueryAsync<Venta>(sql);
    }

    public async Task<Venta?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        const string sqlVenta = @"
            SELECT v.*,
                   c.nombre          AS cliente_nombre,
                   c.apellidos       AS cliente_apellidos,
                   c.email           AS cliente_email,
                   c.nif             AS cliente_nif,
                   f.numero_factura
            FROM ventas v
            JOIN clientes c ON c.id = v.cliente_id
            LEFT JOIN facturas f ON f.venta_id = v.id
            WHERE v.id = @Id";

        var venta = await conn.QuerySingleOrDefaultAsync<Venta>(sqlVenta, new { Id = id });
        if (venta == null) return null;

        var detalles = await conn.QueryAsync<VentaDetalle>(
            "SELECT * FROM venta_detalles WHERE venta_id = @VentaId ORDER BY id",
            new { VentaId = id });

        venta.Detalles = detalles.ToList();
        return venta;
    }

    public async Task<Venta> CreateAsync(Venta venta, List<VentaDetalle> detalles)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            // Número correlativo desde la función PostgreSQL
            var numero = await conn.ExecuteScalarAsync<string>(
                "SELECT generar_numero_venta()", transaction: tx);
            venta.NumeroVenta = numero!;

            const string sqlVenta = @"
                INSERT INTO ventas
                    (numero_venta, cliente_id, base_imponible, porcentaje_iva, importe_iva, total, estado, metodo_pago, observaciones)
                VALUES
                    (@NumeroVenta, @ClienteId, @BaseImponible, @PorcentajeIva, @ImporteIva, @Total, @Estado, @MetodoPago, @Observaciones)
                RETURNING id";

            venta.Id = await conn.ExecuteScalarAsync<int>(sqlVenta, venta, tx);

            foreach (var d in detalles)
            {
                d.VentaId = venta.Id;
                await conn.ExecuteAsync(@"
                    INSERT INTO venta_detalles
                        (venta_id, producto_id, descripcion, cantidad, precio_unitario, subtotal)
                    VALUES
                        (@VentaId, @ProductoId, @Descripcion, @Cantidad, @PrecioUnitario, @Subtotal)",
                    d, tx);

                if (d.ProductoId.HasValue)
                {
                    var stockActual = await conn.ExecuteScalarAsync<int>(
                        "SELECT stock FROM productos WHERE id = @Id FOR UPDATE",
                        new { Id = d.ProductoId.Value }, tx);

                    var nuevoStock = stockActual - d.Cantidad;

                    await conn.ExecuteAsync(
                        "UPDATE productos SET stock = @Stock, fecha_modificacion = NOW() WHERE id = @Id",
                        new { Stock = nuevoStock, Id = d.ProductoId.Value }, tx);

                    await conn.ExecuteAsync(@"
                        INSERT INTO inventario_movimientos
                            (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, referencia_id, observaciones)
                        VALUES
                            (@ProductoId, 'salida', @Cantidad, @StockAnterior, @StockPosterior, 'venta', @VentaId, @Obs)",
                        new
                        {
                            ProductoId    = d.ProductoId.Value,
                            Cantidad      = d.Cantidad,
                            StockAnterior = stockActual,
                            StockPosterior= nuevoStock,
                            VentaId       = venta.Id,
                            Obs           = $"Venta {venta.NumeroVenta}"
                        }, tx);
                }
            }

            tx.Commit();
            venta.Detalles = detalles;
            return venta;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateEstadoAsync(int id, string estado)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE ventas SET estado = @Estado, fecha_modificacion = NOW() WHERE id = @Id",
            new { Estado = estado, Id = id });
    }

    public async Task MarcarFacturaEnviadaAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE ventas SET factura_enviada = true, fecha_modificacion = NOW() WHERE id = @Id",
            new { Id = id });
    }
}
