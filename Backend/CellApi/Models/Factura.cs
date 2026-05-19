namespace CellApi.Models;

public class Factura
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public int? VentaId { get; set; }
    public int? ReparacionId { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteApellidos { get; set; }
    public string? ClienteEmail { get; set; }
    public string? ClienteNif { get; set; }
    public string? ClienteDireccion { get; set; }
    public DateTime FechaEmision { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal PorcentajeIva { get; set; } = 21;
    public decimal ImporteIva { get; set; }
    public decimal Total { get; set; }
    public string? PdfPath { get; set; }
    public bool Anulada { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime FechaCreacion { get; set; }
}
