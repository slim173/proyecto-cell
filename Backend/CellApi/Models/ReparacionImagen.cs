namespace CellApi.Models;

public class ReparacionImagen
{
    public int Id { get; set; }
    public int ReparacionId { get; set; }
    public string RutaImagen { get; set; } = string.Empty;
    public string? NombreArchivo { get; set; }
    public DateTime Fecha { get; set; }
}
