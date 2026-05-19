namespace CellApi.Models;

public class EmailLog
{
    public int Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string? Cuerpo { get; set; }
    public string? Tipo { get; set; }
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
    public string Estado { get; set; } = "pendiente";
    public string? ErrorMensaje { get; set; }
    public int Intentos { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateTime FechaCreacion { get; set; }
}
