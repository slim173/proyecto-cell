namespace CellApi.DTOs;

public class EnviarWhatsAppDto
{
    public int    ClienteId { get; set; }
    public string Mensaje   { get; set; } = string.Empty;
}

public class EnviarWhatsAppMasivoDto
{
    public List<int> ClienteIds { get; set; } = new();
    public string    Mensaje    { get; set; } = string.Empty;
}

public class WhatsAppResultadoDto
{
    public int    ClienteId     { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string Telefono      { get; set; } = string.Empty;
    public bool   Ok            { get; set; }
    public string Mensaje       { get; set; } = string.Empty;
}

public class WhatsAppLogDto
{
    public int      Id            { get; set; }
    public string   Destinatario  { get; set; } = string.Empty;
    public string   ClienteNombre { get; set; } = string.Empty;
    public int?     ClienteId     { get; set; }
    public string   MensajeResumen { get; set; } = string.Empty;
    public string   Estado        { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
