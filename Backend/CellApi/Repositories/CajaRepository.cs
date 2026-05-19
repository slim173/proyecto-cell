using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public interface ICajaRepository
{
    Task<CajaSesion?> GetSesionAbiertaAsync();
    Task<CajaSesion?> GetByIdAsync(int id);
    Task<IEnumerable<CajaSesion>> GetHistorialAsync(int limit = 30);
    Task<int> AbrirSesionAsync(CajaSesion sesion);
    Task CerrarSesionAsync(int id, decimal efectivoCierre, decimal diferencia, string? obs, string? usuario);
    Task<int> AddMovimientoAsync(CajaMovimiento mov);
    Task ActualizarTotalesAsync(int sesionId);
}

public class CajaRepository : ICajaRepository
{
    private readonly DbConnectionFactory _db;
    public CajaRepository(DbConnectionFactory db) => _db = db;

    public async Task<CajaSesion?> GetSesionAbiertaAsync()
    {
        using var conn = _db.CreateConnection();
        var sesion = await conn.QueryFirstOrDefaultAsync<CajaSesion>(
            "SELECT * FROM caja_sesiones WHERE estado = 'abierta' ORDER BY fecha_apertura DESC LIMIT 1");
        if (sesion != null)
            sesion.Movimientos = (await conn.QueryAsync<CajaMovimiento>(
                "SELECT * FROM caja_movimientos WHERE sesion_id = @Id ORDER BY fecha",
                new { sesion.Id })).ToList();
        return sesion;
    }

    public async Task<CajaSesion?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var sesion = await conn.QueryFirstOrDefaultAsync<CajaSesion>(
            "SELECT * FROM caja_sesiones WHERE id = @Id", new { Id = id });
        if (sesion != null)
            sesion.Movimientos = (await conn.QueryAsync<CajaMovimiento>(
                "SELECT * FROM caja_movimientos WHERE sesion_id = @Id ORDER BY fecha",
                new { sesion.Id })).ToList();
        return sesion;
    }

    public async Task<IEnumerable<CajaSesion>> GetHistorialAsync(int limit = 30)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<CajaSesion>(
            "SELECT * FROM caja_sesiones ORDER BY fecha_apertura DESC LIMIT @Limit",
            new { Limit = limit });
    }

    public async Task<int> AbrirSesionAsync(CajaSesion s)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO caja_sesiones
                (numero_sesion, efectivo_apertura, estado, usuario_apertura)
            VALUES
                (generar_numero_sesion_caja(), @EfectivoApertura, 'abierta', @UsuarioApertura)
            RETURNING id", s);
    }

    public async Task CerrarSesionAsync(int id, decimal efectivoCierre, decimal diferencia,
        string? obs, string? usuario)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE caja_sesiones SET
                fecha_cierre    = NOW(),
                efectivo_cierre = @EfectivoCierre,
                diferencia      = @Diferencia,
                estado          = 'cerrada',
                observaciones   = @Obs,
                usuario_cierre  = @Usuario
            WHERE id = @Id",
            new { Id = id, EfectivoCierre = efectivoCierre, Diferencia = diferencia,
                  Obs = obs, Usuario = usuario });
    }

    public async Task<int> AddMovimientoAsync(CajaMovimiento m)
    {
        using var conn = _db.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO caja_movimientos
                (sesion_id, tipo, concepto, importe, metodo_pago,
                 referencia_tipo, referencia_id, usuario)
            VALUES
                (@SesionId, @Tipo, @Concepto, @Importe, @MetodoPago,
                 @ReferenciaTipo, @ReferenciaId, @Usuario)
            RETURNING id", m);
        await ActualizarTotalesAsync(m.SesionId);
        return id;
    }

    public async Task ActualizarTotalesAsync(int sesionId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE caja_sesiones SET
                total_efectivo = COALESCE((
                    SELECT SUM(CASE WHEN tipo IN ('entrada','venta') THEN importe
                                    WHEN tipo IN ('salida','devolucion') THEN -importe
                                    ELSE 0 END)
                    FROM caja_movimientos
                    WHERE sesion_id = @Id AND (metodo_pago = 'efectivo' OR metodo_pago IS NULL)
                ), 0),
                total_tarjeta = COALESCE((
                    SELECT SUM(importe) FROM caja_movimientos
                    WHERE sesion_id = @Id AND metodo_pago = 'tarjeta'
                      AND tipo IN ('entrada','venta')
                ), 0),
                total_otros = COALESCE((
                    SELECT SUM(importe) FROM caja_movimientos
                    WHERE sesion_id = @Id
                      AND metodo_pago NOT IN ('efectivo','tarjeta')
                      AND metodo_pago IS NOT NULL
                      AND tipo IN ('entrada','venta')
                ), 0)
            WHERE id = @Id", new { Id = sesionId });
    }
}
