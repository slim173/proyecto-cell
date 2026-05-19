namespace CellApi.Models;

public class Compra
{
    public int Id { get; set; }
    public string NumeroCompra { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ProveedorTelefono { get; set; }
    public string? ProveedorEmail { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "pendiente";
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public List<CompraDetalle> Detalles { get; set; } = new();
}
