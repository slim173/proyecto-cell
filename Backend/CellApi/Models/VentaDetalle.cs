namespace CellApi.Models;

public class VentaDetalle
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int? ProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
