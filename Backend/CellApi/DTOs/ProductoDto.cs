using System.ComponentModel.DataAnnotations;

namespace CellApi.DTOs;

public class ProductoDto
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Costo { get; set; }
    public decimal Margen => Costo > 0 ? Math.Round((PrecioVenta - Costo) / Costo * 100, 2) : 0;
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public bool StockBajo => Stock <= StockMinimo;
    public string UnidadMedida { get; set; } = "unidad";
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CategoriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}

public class CreateProductoDto
{
    [MaxLength(50)]
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int? CategoriaId { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a 0")]
    public decimal PrecioVenta { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El costo debe ser mayor o igual a 0")]
    public decimal Costo { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int StockMinimo { get; set; } = 0;

    [MaxLength(30)]
    public string UnidadMedida { get; set; } = "unidad";
}

public class UpdateProductoDto
{
    [MaxLength(50)]
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int? CategoriaId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal PrecioVenta { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Costo { get; set; }

    [Range(0, int.MaxValue)]
    public int StockMinimo { get; set; } = 0;

    [MaxLength(30)]
    public string UnidadMedida { get; set; } = "unidad";

    public bool Activo { get; set; } = true;
}

public class AjusteStockDto
{
    [Required]
    public int ProductoId { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int NuevoStock { get; set; }

    public string? Observaciones { get; set; }
}
