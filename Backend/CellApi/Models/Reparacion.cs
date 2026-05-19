namespace CellApi.Models;

public class Reparacion
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteApellidos { get; set; }
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }
    public string Dispositivo { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Imei { get; set; }
    public string DescripcionFalla { get; set; } = string.Empty;
    public string? ObservacionesTecnico { get; set; }
    public string? Solucion { get; set; }
    public string Estado { get; set; } = "recibido";
    public string Prioridad { get; set; } = "normal";
    public decimal? PrecioEstimado { get; set; }
    public decimal? PrecioFinal { get; set; }
    public decimal? BaseImponible { get; set; }
    public decimal PorcentajeIva { get; set; } = 21;
    public decimal? ImporteIva { get; set; }
    public decimal? Total { get; set; }
    public string? TecnicoAsignado { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public DateTime? FechaEstimadaEntrega { get; set; }
    public DateTime? FechaEntregaReal { get; set; }
    public bool FacturaEnviada { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public List<ReparacionDetalle> Detalles { get; set; } = new();
    public List<ReparacionImagen> Imagenes { get; set; } = new();
}
