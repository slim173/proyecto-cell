using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public class ReparacionRepository : IReparacionRepository
{
    private readonly DbConnectionFactory _db;

    public ReparacionRepository(DbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Reparacion>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT r.*,
                   c.nombre    AS cliente_nombre,
                   c.apellidos AS cliente_apellidos,
                   c.email     AS cliente_email,
                   c.telefono  AS cliente_telefono
            FROM reparaciones r
            JOIN clientes c ON c.id = r.cliente_id
            ORDER BY r.fecha_recepcion DESC";
        return await conn.QueryAsync<Reparacion>(sql);
    }

    public async Task<Reparacion?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT r.*,
                   c.nombre    AS cliente_nombre,
                   c.apellidos AS cliente_apellidos,
                   c.email     AS cliente_email,
                   c.telefono  AS cliente_telefono
            FROM reparaciones r
            JOIN clientes c ON c.id = r.cliente_id
            WHERE r.id = @Id";

        var rep = await conn.QuerySingleOrDefaultAsync<Reparacion>(sql, new { Id = id });
        if (rep == null) return null;

        var detalles = await conn.QueryAsync<ReparacionDetalle>(
            "SELECT * FROM reparacion_detalles WHERE reparacion_id = @Id ORDER BY id",
            new { Id = id });

        var imagenes = await conn.QueryAsync<ReparacionImagen>(
            "SELECT * FROM reparacion_imagenes WHERE reparacion_id = @Id ORDER BY fecha",
            new { Id = id });

        rep.Detalles  = detalles.ToList();
        rep.Imagenes  = imagenes.ToList();
        return rep;
    }

    public async Task<int> CreateAsync(Reparacion r)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var numero = await conn.ExecuteScalarAsync<string>(
                "SELECT generar_numero_reparacion()", transaction: tx);
            r.NumeroOrden = numero!;

            const string sql = @"
                INSERT INTO reparaciones
                    (numero_orden, cliente_id, dispositivo, marca, modelo, imei,
                     descripcion_falla, estado, prioridad, precio_estimado,
                     tecnico_asignado, fecha_estimada_entrega)
                VALUES
                    (@NumeroOrden, @ClienteId, @Dispositivo, @Marca, @Modelo, @Imei,
                     @DescripcionFalla, @Estado, @Prioridad, @PrecioEstimado,
                     @TecnicoAsignado, @FechaEstimadaEntrega)
                RETURNING id";

            var newId = await conn.ExecuteScalarAsync<int>(sql, r, tx);
            tx.Commit();
            return newId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(Reparacion r)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            UPDATE reparaciones SET
                cliente_id              = @ClienteId,
                dispositivo             = @Dispositivo,
                marca                   = @Marca,
                modelo                  = @Modelo,
                imei                    = @Imei,
                descripcion_falla       = @DescripcionFalla,
                prioridad               = @Prioridad,
                precio_estimado         = @PrecioEstimado,
                tecnico_asignado        = @TecnicoAsignado,
                fecha_estimada_entrega  = @FechaEstimadaEntrega,
                fecha_modificacion      = NOW()
            WHERE id = @Id";
        await conn.ExecuteAsync(sql, r);
    }

    public async Task UpdateEstadoAsync(Reparacion r)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            UPDATE reparaciones SET
                estado                  = @Estado,
                observaciones_tecnico   = @ObservacionesTecnico,
                solucion                = @Solucion,
                precio_estimado         = @PrecioEstimado,
                precio_final            = @PrecioFinal,
                tecnico_asignado        = @TecnicoAsignado,
                fecha_estimada_entrega  = @FechaEstimadaEntrega,
                fecha_entrega_real      = @FechaEntregaReal,
                fecha_modificacion      = NOW()
            WHERE id = @Id";
        await conn.ExecuteAsync(sql, r);
    }

    public async Task<IEnumerable<Reparacion>> GetHistorialByImeiAsync(string imei, int excluirId)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT r.id, r.numero_orden, r.estado, r.fecha_recepcion, r.fecha_entrega_real,
                   r.descripcion_falla, r.solucion, r.tecnico_asignado, r.total
            FROM reparaciones r
            WHERE TRIM(UPPER(r.imei)) = TRIM(UPPER(@Imei))
              AND r.id <> @ExcluirId
            ORDER BY r.fecha_recepcion DESC";
        return await conn.QueryAsync<Reparacion>(sql, new { Imei = imei, ExcluirId = excluirId });
    }

    public async Task ActualizarTotalesAsync(int id, decimal baseImponible, decimal porcentajeIva,
        decimal importeIva, decimal total, decimal precioFinal)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE reparaciones SET
                base_imponible     = @BaseImponible,
                porcentaje_iva     = @PorcentajeIva,
                importe_iva        = @ImporteIva,
                total              = @Total,
                precio_final       = @PrecioFinal,
                fecha_modificacion = NOW()
            WHERE id = @Id",
            new { Id = id, BaseImponible = baseImponible, PorcentajeIva = porcentajeIva,
                  ImporteIva = importeIva, Total = total, PrecioFinal = precioFinal });
    }

    public async Task<int> AddDetalleAsync(ReparacionDetalle d)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            INSERT INTO reparacion_detalles
                (reparacion_id, producto_id, descripcion, cantidad, precio_unitario, subtotal)
            VALUES
                (@ReparacionId, @ProductoId, @Descripcion, @Cantidad, @PrecioUnitario, @Subtotal)
            RETURNING id";
        return await conn.ExecuteScalarAsync<int>(sql, d);
    }

    public async Task RemoveDetalleAsync(int detalleId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM reparacion_detalles WHERE id = @Id", new { Id = detalleId });
    }

    public async Task<IEnumerable<ReparacionImagen>> GetImagenesAsync(int reparacionId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ReparacionImagen>(
            "SELECT * FROM reparacion_imagenes WHERE reparacion_id = @Id ORDER BY fecha",
            new { Id = reparacionId });
    }

    public async Task<int> AddImagenAsync(ReparacionImagen imagen)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            INSERT INTO reparacion_imagenes (reparacion_id, ruta_imagen, nombre_archivo)
            VALUES (@ReparacionId, @RutaImagen, @NombreArchivo)
            RETURNING id";
        return await conn.ExecuteScalarAsync<int>(sql, imagen);
    }

    public async Task<int> CountImagenesAsync(int reparacionId)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM reparacion_imagenes WHERE reparacion_id = @Id",
            new { Id = reparacionId });
    }

    public async Task DeleteImagenAsync(int imagenId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM reparacion_imagenes WHERE id = @Id", new { Id = imagenId });
    }

    public async Task MarcarFacturaEnviadaAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE reparaciones SET factura_enviada = true, fecha_modificacion = NOW() WHERE id = @Id",
            new { Id = id });
    }

    public async Task<Reparacion?> GetByNumeroOrdenAsync(string numeroOrden)
    {
        using var conn = _db.CreateConnection();
        var rep = await conn.QueryFirstOrDefaultAsync<Reparacion>(
            "SELECT r.* FROM reparaciones r WHERE UPPER(r.numero_orden) = UPPER(@NumeroOrden)",
            new { NumeroOrden = numeroOrden });
        if (rep == null) return null;
        rep.Detalles = (await conn.QueryAsync<ReparacionDetalle>(
            "SELECT * FROM reparacion_detalles WHERE reparacion_id = @Id ORDER BY id",
            new { Id = rep.Id })).ToList();
        return rep;
    }

    public async Task<IEnumerable<Reparacion>> GetReparadasSinRecogerAsync(int diasLimite)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Reparacion>(@"
            SELECT r.* FROM reparaciones r
            WHERE r.estado = 'reparado'
              AND r.recordatorio_enviado = false
              AND r.fecha_modificacion < NOW() - (@Dias || ' days')::INTERVAL",
            new { Dias = diasLimite });
    }

    public async Task MarcarRecordatorioEnviadoAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE reparaciones SET recordatorio_enviado = true, fecha_modificacion = NOW() WHERE id = @Id",
            new { Id = id });
    }
}
