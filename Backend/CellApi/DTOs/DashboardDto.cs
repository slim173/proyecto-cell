namespace CellApi.DTOs;

public class DashboardDto
{
    public ResumenVentasDto VentasHoy { get; set; } = new();
    public ResumenVentasDto VentasMes { get; set; } = new();
    public ResumenVentasDto VentasAnio { get; set; } = new();
    public int ReparacionesAbiertas { get; set; }
    public int ReparacionesEntregadasHoy { get; set; }
    public int ProductosStockBajo { get; set; }
    public List<VentaResumenDto> UltimasVentas { get; set; } = new();
    public List<ReparacionResumenDto> UltimasReparaciones { get; set; } = new();
    public List<ProductoStockBajoDto> AlertasStock { get; set; } = new();
    public List<DiaVentasDto> VentasUltimos7Dias { get; set; } = new();
    public List<EstadoReparacionDto> ReparacionesPorEstado { get; set; } = new();
}

public class DiaVentasDto
{
    public string Fecha { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Cantidad { get; set; }
}

public class EstadoReparacionDto
{
    public string Estado { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class ResumenVentasDto
{
    public int TotalVentas { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal IvaTotal { get; set; }
    public decimal TicketMedio { get; set; }
}

public class VentaResumenDto
{
    public int Id { get; set; }
    public string NumeroVenta { get; set; } = string.Empty;
    public string? ClienteNombre { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MetodoPago { get; set; }
}

public class ReparacionResumenDto
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public string? ClienteNombre { get; set; }
    public string Dispositivo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public DateTime FechaRecepcion { get; set; }
}

public class ProductoStockBajoDto
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public int UnidadesFaltantes { get; set; }
    public string? Categoria { get; set; }
}
