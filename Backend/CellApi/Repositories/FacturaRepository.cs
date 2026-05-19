using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public class FacturaRepository : IFacturaRepository
{
    private readonly DbConnectionFactory _db;

    public FacturaRepository(DbConnectionFactory db) => _db = db;

    private const string SelectBase = @"
        SELECT f.*,
               c.nombre    AS cliente_nombre,
               c.apellidos AS cliente_apellidos,
               c.email     AS cliente_email,
               c.nif       AS cliente_nif,
               c.direccion AS cliente_direccion
        FROM facturas f
        JOIN clientes c ON c.id = f.cliente_id";

    public async Task<IEnumerable<Factura>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Factura>($"{SelectBase} ORDER BY f.fecha_emision DESC, f.numero_factura DESC");
    }

    public async Task<Factura?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Factura>(
            $"{SelectBase} WHERE f.id = @Id", new { Id = id });
    }

    public async Task<Factura?> GetByVentaIdAsync(int ventaId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Factura>(
            $"{SelectBase} WHERE f.venta_id = @VentaId", new { VentaId = ventaId });
    }

    public async Task<Factura?> GetByReparacionIdAsync(int reparacionId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Factura>(
            $"{SelectBase} WHERE f.reparacion_id = @ReparacionId", new { ReparacionId = reparacionId });
    }

    public async Task<int> CreateAsync(Factura f)
    {
        using var conn = _db.CreateConnection();
        var numero = await conn.ExecuteScalarAsync<string>("SELECT generar_numero_factura()");
        f.NumeroFactura = numero!;

        const string sql = @"
            INSERT INTO facturas
                (numero_factura, venta_id, reparacion_id, cliente_id, fecha_emision,
                 base_imponible, porcentaje_iva, importe_iva, total)
            VALUES
                (@NumeroFactura, @VentaId, @ReparacionId, @ClienteId, @FechaEmision,
                 @BaseImponible, @PorcentajeIva, @ImporteIva, @Total)
            RETURNING id";

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            f.NumeroFactura,
            VentaId       = (object?)(f.VentaId.HasValue       ? f.VentaId       : null) ?? DBNull.Value,
            ReparacionId  = (object?)(f.ReparacionId.HasValue  ? f.ReparacionId  : null) ?? DBNull.Value,
            f.ClienteId,
            FechaEmision  = f.FechaEmision.Date,
            f.BaseImponible,
            f.PorcentajeIva,
            f.ImporteIva,
            f.Total
        });
    }

    public async Task UpdatePdfPathAsync(int id, string pdfPath)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE facturas SET pdf_path = @PdfPath WHERE id = @Id",
            new { PdfPath = pdfPath, Id = id });
    }

    public async Task AnularAsync(int id, string motivo)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE facturas SET anulada = true, motivo_anulacion = @Motivo WHERE id = @Id",
            new { Motivo = motivo, Id = id });
    }
}
