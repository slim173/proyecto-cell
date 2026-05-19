using System.ComponentModel.DataAnnotations;

namespace CellApi.DTOs;

public class ReparacionDto
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteApellidos { get; set; }
    public string? ClienteNombreCompleto => $"{ClienteNombre} {ClienteApellidos}".Trim();
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }
    public string Dispositivo { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Imei { get; set; }
    public string DescripcionFalla { get; set; } = string.Empty;
    public string? ObservacionesTecnico { get; set; }
    public string? Solucion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public decimal? PrecioEstimado { get; set; }
    public decimal? PrecioFinal { get; set; }
    public decimal? BaseImponible { get; set; }
    public decimal PorcentajeIva { get; set; }
    public decimal? ImporteIva { get; set; }
    public decimal? Total { get; set; }
    public string? TecnicoAsignado { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public DateTime? FechaEstimadaEntrega { get; set; }
    public DateTime? FechaEntregaReal { get; set; }
    public bool FacturaEnviada { get; set; }
    public List<ReparacionDetalleDto> Detalles { get; set; } = new();
    public List<ReparacionImagenDto> Imagenes { get; set; } = new();
}

public class ReparacionImagenDto
{
    public int Id { get; set; }
    public int ReparacionId { get; set; }
    public string RutaImagen { get; set; } = string.Empty;
    public string? NombreArchivo { get; set; }
    public DateTime Fecha { get; set; }
}

public class ReparacionDetalleDto
{
    public int Id { get; set; }
    public int? ProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreateReparacionDto
{
    [Required(ErrorMessage = "El cliente es obligatorio")]
    public int ClienteId { get; set; }

    [Required(ErrorMessage = "El dispositivo es obligatorio")]
    [MaxLength(200)]
    public string Dispositivo { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Marca { get; set; }

    [MaxLength(100)]
    public string? Modelo { get; set; }

    [MaxLength(20)]
    public string? Imei { get; set; }

    [Required(ErrorMessage = "La descripción de la falla es obligatoria")]
    public string DescripcionFalla { get; set; } = string.Empty;

    public string Prioridad { get; set; } = "normal";

    [Range(0, double.MaxValue)]
    public decimal? PrecioEstimado { get; set; }

    [MaxLength(150)]
    public string? TecnicoAsignado { get; set; }

    public DateTime? FechaEstimadaEntrega { get; set; }
}

// Resumen ligero para historial de equipo
public class HistorialEquipoDto
{
    public int      Id           { get; set; }
    public string   NumeroOrden  { get; set; } = string.Empty;
    public string   Estado       { get; set; } = string.Empty;
    public DateTime FechaRecepcion { get; set; }
    public DateTime? FechaEntregaReal { get; set; }
    public string   DescripcionFalla  { get; set; } = string.Empty;
    public string?  Solucion     { get; set; }
    public string?  TecnicoAsignado { get; set; }
    public decimal? Total        { get; set; }
}

public class UpdateReparacionDto
{
    [Required(ErrorMessage = "El cliente es obligatorio")]
    public int ClienteId { get; set; }

    [Required(ErrorMessage = "El dispositivo es obligatorio")]
    [MaxLength(200)]
    public string Dispositivo { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Marca { get; set; }

    [MaxLength(100)]
    public string? Modelo { get; set; }

    [MaxLength(20)]
    public string? Imei { get; set; }

    [Required(ErrorMessage = "La descripción de la falla es obligatoria")]
    public string DescripcionFalla { get; set; } = string.Empty;

    public string Prioridad { get; set; } = "normal";

    [Range(0, double.MaxValue)]
    public decimal? PrecioEstimado { get; set; }

    [MaxLength(150)]
    public string? TecnicoAsignado { get; set; }

    public DateTime? FechaEstimadaEntrega { get; set; }
}

public class UpdateReparacionEstadoDto
{
    [Required(ErrorMessage = "El estado es obligatorio")]
    public string Estado { get; set; } = string.Empty;

    public string? ObservacionesTecnico { get; set; }
    public string? Solucion { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? PrecioEstimado { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? PrecioFinal { get; set; }

    [MaxLength(150)]
    public string? TecnicoAsignado { get; set; }

    public DateTime? FechaEstimadaEntrega { get; set; }
}

public class AddReparacionDetalleDto
{
    public int? ProductoId { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [MaxLength(300)]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; } = 1;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal PrecioUnitario { get; set; }
}
