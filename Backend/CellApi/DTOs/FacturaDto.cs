namespace CellApi.DTOs;

public class FacturaDto
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public int? VentaId { get; set; }
    public int? ReparacionId { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteApellidos { get; set; }
    public string? ClienteNombreCompleto => $"{ClienteNombre} {ClienteApellidos}".Trim();
    public string? ClienteEmail { get; set; }
    public string? ClienteNif { get; set; }
    public string? ClienteDireccion { get; set; }
    public DateTime FechaEmision { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal PorcentajeIva { get; set; }
    public decimal ImporteIva { get; set; }
    public decimal Total { get; set; }
    public string? PdfPath { get; set; }
    public bool Anulada { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<FacturaLineaDto> Lineas { get; set; } = new();
}

public class FacturaLineaDto
{
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class AnularFacturaDto
{
    public string MotivoAnulacion { get; set; } = string.Empty;
}

public class CreateFacturaDto
{
    public int ClienteId { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.Today;
    public List<CreateFacturaLineaDto> Lineas { get; set; } = new();
}

public class CreateFacturaLineaDto
{
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
}

public class CrearFacturaResponseDto
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
}
