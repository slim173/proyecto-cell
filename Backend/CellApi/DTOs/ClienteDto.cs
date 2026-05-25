using System.ComponentModel.DataAnnotations;

namespace CellApi.DTOs;

public class ClienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Nif { get; set; }
    public bool Activo { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateClienteDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Apellidos { get; set; }

    [EmailAddress(ErrorMessage = "Formato de email no válido")]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    [MaxLength(100)]
    public string? Ciudad { get; set; }

    [MaxLength(10)]
    public string? CodigoPostal { get; set; }

    [MaxLength(20)]
    public string? Nif { get; set; }

    public string? Observaciones { get; set; }
}

public class UpdateClienteDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Apellidos { get; set; }

    [EmailAddress(ErrorMessage = "Formato de email no válido")]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    [MaxLength(100)]
    public string? Ciudad { get; set; }

    [MaxLength(10)]
    public string? CodigoPostal { get; set; }

    [MaxLength(20)]
    public string? Nif { get; set; }

    public string? Observaciones { get; set; }

    public bool Activo { get; set; } = true;
}
