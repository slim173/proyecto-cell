namespace CellApi.Models;

public class Producto
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Costo { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public string UnidadMedida { get; set; } = "unidad";
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
