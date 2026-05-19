using System.ComponentModel.DataAnnotations;

namespace CellApi.DTOs;

public class VentaDto
{
    public int Id { get; set; }
    public string NumeroVenta { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteApellidos { get; set; }
    public string? ClienteNombreCompleto => $"{ClienteNombre} {ClienteApellidos}".Trim();
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }
    public string? ClienteNif { get; set; }
    public DateTime Fecha { get; set; }
    public decimal BaseImponible  { get; set; }
    public decimal PorcentajeIva  { get; set; }
    public decimal ImporteIva     { get; set; }
    public decimal Descuento      { get; set; }
    public string  TipoDescuento  { get; set; } = "importe";
    public decimal Total          { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MetodoPago { get; set; }
    public string? Observaciones { get; set; }
    public bool FacturaEnviada { get; set; }
    public string? NumeroFactura { get; set; }
    public List<VentaDetalleDto> Detalles { get; set; } = new();
}

public class VentaDetalleDto
{
    public int Id { get; set; }
    public int? ProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreateVentaDto
{
    [Required(ErrorMessage = "El cliente es obligatorio")]
    public int ClienteId { get; set; }

    [MaxLength(50)]
    public string MetodoPago { get; set; } = "efectivo";

    public string? Observaciones { get; set; }

    public decimal Descuento     { get; set; }
    public string  TipoDescuento { get; set; } = "importe";

    [Required(ErrorMessage = "Debe incluir al menos un producto")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un producto")]
    public List<CreateVentaDetalleDto> Detalles { get; set; } = new();
}

public class CreateVentaDetalleDto
{
    public int? ProductoId { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [MaxLength(300)]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public int Cantidad { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal PrecioUnitario { get; set; }
}

public class UpdateVentaEstadoDto
{
    [Required(ErrorMessage = "El estado es obligatorio")]
    public string Estado { get; set; } = string.Empty;
}
