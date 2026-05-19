using CellApi.Data;
using CellApi.DTOs;
using Dapper;

namespace CellApi.Services;

public class DashboardService : IDashboardService
{
    private readonly DbConnectionFactory _db;

    public DashboardService(DbConnectionFactory db) => _db = db;

    public async Task<DashboardDto> GetDashboardAsync()
    {
        using var conn = _db.CreateConnection();

        var dashboard = new DashboardDto();

        // ── Ventas hoy ──────────────────────────────────────────
        var ventasHoy = await conn.QueryFirstOrDefaultAsync(@"
            SELECT
                COUNT(*)::INT                    AS total_ventas,
                COALESCE(SUM(total), 0)          AS importe_total,
                COALESCE(SUM(importe_iva), 0)    AS iva_total,
                COALESCE(AVG(total), 0)          AS ticket_medio
            FROM ventas
            WHERE DATE(fecha) = CURRENT_DATE AND estado != 'anulada'");

        dashboard.VentasHoy = MapResumen(ventasHoy);

        // ── Ventas mes ───────────────────────────────────────────
        var ventasMes = await conn.QueryFirstOrDefaultAsync(@"
            SELECT
                COUNT(*)::INT                    AS total_ventas,
                COALESCE(SUM(total), 0)          AS importe_total,
                COALESCE(SUM(importe_iva), 0)    AS iva_total,
                COALESCE(AVG(total), 0)          AS ticket_medio
            FROM ventas
            WHERE DATE_TRUNC('month', fecha) = DATE_TRUNC('month', CURRENT_DATE)
              AND estado != 'anulada'");

        dashboard.VentasMes = MapResumen(ventasMes);

        // ── Ventas año ───────────────────────────────────────────
        var ventasAnio = await conn.QueryFirstOrDefaultAsync(@"
            SELECT
                COUNT(*)::INT                    AS total_ventas,
                COALESCE(SUM(total), 0)          AS importe_total,
                COALESCE(SUM(importe_iva), 0)    AS iva_total,
                COALESCE(AVG(total), 0)          AS ticket_medio
            FROM ventas
            WHERE DATE_TRUNC('year', fecha) = DATE_TRUNC('year', CURRENT_DATE)
              AND estado != 'anulada'");

        dashboard.VentasAnio = MapResumen(ventasAnio);

        // ── Reparaciones abiertas ─────────────────────────────────
        dashboard.ReparacionesAbiertas = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)::INT FROM reparaciones
            WHERE estado NOT IN ('entregado', 'no_reparable')");

        dashboard.ReparacionesEntregadasHoy = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)::INT FROM reparaciones
            WHERE estado = 'entregado' AND DATE(fecha_entrega_real) = CURRENT_DATE");

        // ── Productos stock bajo ──────────────────────────────────
        dashboard.ProductosStockBajo = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)::INT FROM productos
            WHERE activo = true AND stock <= stock_minimo");

        // ── Últimas 5 ventas ──────────────────────────────────────
        var ultimasVentas = await conn.QueryAsync(@"
            SELECT v.id, v.numero_venta, v.fecha, v.total, v.estado, v.metodo_pago,
                   CONCAT(c.nombre, ' ', COALESCE(c.apellidos,'')) AS cliente_nombre
            FROM ventas v
            JOIN clientes c ON c.id = v.cliente_id
            ORDER BY v.fecha DESC
            LIMIT 5");

        dashboard.UltimasVentas = ultimasVentas.Select(r => new VentaResumenDto
        {
            Id           = (int)r.id,
            NumeroVenta  = r.numero_venta,
            ClienteNombre= r.cliente_nombre,
            Fecha        = r.fecha,
            Total        = r.total,
            Estado       = r.estado,
            MetodoPago   = r.metodo_pago
        }).ToList();

        // ── Últimas 5 reparaciones ────────────────────────────────
        var ultimasRep = await conn.QueryAsync(@"
            SELECT r.id, r.numero_orden, r.dispositivo, r.estado, r.prioridad, r.fecha_recepcion,
                   CONCAT(c.nombre, ' ', COALESCE(c.apellidos,'')) AS cliente_nombre
            FROM reparaciones r
            JOIN clientes c ON c.id = r.cliente_id
            ORDER BY r.fecha_recepcion DESC
            LIMIT 5");

        dashboard.UltimasReparaciones = ultimasRep.Select(r => new ReparacionResumenDto
        {
            Id            = (int)r.id,
            NumeroOrden   = r.numero_orden,
            ClienteNombre = r.cliente_nombre,
            Dispositivo   = r.dispositivo,
            Estado        = r.estado,
            Prioridad     = r.prioridad,
            FechaRecepcion= r.fecha_recepcion
        }).ToList();

        // ── Alertas stock ─────────────────────────────────────────
        var alertas = await conn.QueryAsync(@"
            SELECT p.id, p.codigo, p.nombre, p.stock, p.stock_minimo,
                   (p.stock_minimo - p.stock) AS unidades_faltantes,
                   cat.nombre AS categoria
            FROM productos p
            LEFT JOIN categorias cat ON cat.id = p.categoria_id
            WHERE p.activo = true AND p.stock <= p.stock_minimo
            ORDER BY (p.stock_minimo - p.stock) DESC
            LIMIT 10");

        dashboard.AlertasStock = alertas.Select(r => new ProductoStockBajoDto
        {
            Id               = (int)r.id,
            Codigo           = r.codigo,
            Nombre           = r.nombre,
            Stock            = (int)r.stock,
            StockMinimo      = (int)r.stock_minimo,
            UnidadesFaltantes= (int)r.unidades_faltantes,
            Categoria        = r.categoria
        }).ToList();

        // ── Ventas últimos 7 días (para gráfica de línea) ─────────────
        var rawDias = await conn.QueryAsync(@"
            SELECT DATE(fecha) AS dia,
                   COUNT(*)::INT AS cantidad,
                   COALESCE(SUM(total), 0) AS total
            FROM ventas
            WHERE fecha >= CURRENT_DATE - INTERVAL '6 days'
              AND estado != 'anulada'
            GROUP BY DATE(fecha)
            ORDER BY dia ASC");

        var diasDict = new Dictionary<DateTime, (decimal total, int cantidad)>();
        foreach (var r in rawDias)
            diasDict[((DateTime)r.dia).Date] = ((decimal)r.total, (int)r.cantidad);

        var hoy = DateTime.Today;
        dashboard.VentasUltimos7Dias = Enumerable.Range(0, 7).Select(i =>
        {
            var dia = hoy.AddDays(i - 6);
            diasDict.TryGetValue(dia.Date, out var v);
            return new DiaVentasDto { Fecha = dia.ToString("dd/MM"), Total = v.total, Cantidad = v.cantidad };
        }).ToList();

        // ── Reparaciones por estado (para gráfica de dona) ────────────
        var rawEstados = await conn.QueryAsync(@"
            SELECT estado, COUNT(*)::INT AS cantidad
            FROM reparaciones
            GROUP BY estado
            ORDER BY cantidad DESC");

        dashboard.ReparacionesPorEstado = rawEstados.Select(r => new EstadoReparacionDto
        {
            Estado   = r.estado,
            Cantidad = (int)r.cantidad
        }).ToList();

        return dashboard;
    }

    private static ResumenVentasDto MapResumen(dynamic? row)
    {
        if (row == null) return new ResumenVentasDto();
        return new ResumenVentasDto
        {
            TotalVentas  = (int)row.total_ventas,
            ImporteTotal = (decimal)row.importe_total,
            IvaTotal     = (decimal)row.iva_total,
            TicketMedio  = (decimal)row.ticket_medio
        };
    }
}
