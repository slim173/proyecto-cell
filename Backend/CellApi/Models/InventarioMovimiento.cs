namespace CellApi.Models;

public class InventarioMovimiento
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? ProductoNombre { get; set; }
    public string? ProductoCodigo { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockPosterior { get; set; }
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
    public string? Observaciones { get; set; }
    public string? Usuario { get; set; }
    public DateTime Fecha { get; set; }
}
