using System.ComponentModel.DataAnnotations;

namespace CellApi.DTOs;

public class CompraDto
{
    public int Id { get; set; }
    public string NumeroCompra { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ProveedorTelefono { get; set; }
    public string? ProveedorEmail { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<CompraDetalleDto> Detalles { get; set; } = new();
}

public class CompraDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? ProductoNombre { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class ProveedorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Cif { get; set; }
    public bool Activo { get; set; }
}

public class CreateCompraDto
{
    [Required(ErrorMessage = "El proveedor es obligatorio")]
    public int ProveedorId { get; set; }

    public string? Observaciones { get; set; }

    [Required(ErrorMessage = "Debe incluir al menos un producto")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un producto")]
    public List<CreateCompraDetalleDto> Detalles { get; set; } = new();
}

public class CreateCompraDetalleDto
{
    [Required(ErrorMessage = "El producto es obligatorio")]
    public int ProductoId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public int Cantidad { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El costo debe ser mayor o igual a 0")]
    public decimal CostoUnitario { get; set; }
}

public class CreateProveedorDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    [MaxLength(100)]
    public string? Ciudad { get; set; }

    [MaxLength(20)]
    public string? Cif { get; set; }

    public string? Observaciones { get; set; }
}

public class UpdateProveedorDto : CreateProveedorDto
{
    public bool Activo { get; set; } = true;
}
