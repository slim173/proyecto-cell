namespace CellApi.Models;

public class Venta
{
    public int Id { get; set; }
    public string NumeroVenta { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteApellidos { get; set; }
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }
    public string? ClienteNif { get; set; }
    public DateTime Fecha { get; set; }
    public decimal BaseImponible  { get; set; }
    public decimal PorcentajeIva  { get; set; } = 21;
    public decimal ImporteIva     { get; set; }
    public decimal Descuento      { get; set; }
    public string  TipoDescuento  { get; set; } = "importe";
    public decimal Total          { get; set; }
    public string Estado { get; set; } = "pendiente";
    public string? MetodoPago { get; set; }
    public string? Observaciones { get; set; }
    public bool FacturaEnviada { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? NumeroFactura { get; set; }
    public List<VentaDetalle> Detalles { get; set; } = new();
}
