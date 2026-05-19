using System.ComponentModel.DataAnnotations;

namespace CellApi.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "El username es obligatorio")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = "empleado";
    public DateTime Expiration { get; set; }
}

public class UsuarioAdminDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Rol { get; set; } = "empleado";
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateUsuarioDto
{
    [Required(ErrorMessage = "El username es obligatorio")]
    public string Username { get; set; } = string.Empty;
    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = string.Empty;
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Rol { get; set; } = "empleado";
}

public class UpdateUsuarioAdminDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Rol { get; set; } = "empleado";
    public bool Activo { get; set; } = true;
    public string? PasswordNueva { get; set; }
}

public class PerfilDto
{
    public string  Username          { get; set; } = string.Empty;
    public string  Nombre            { get; set; } = string.Empty;
    public string? Email             { get; set; }
    public bool    TieneSmtpPassword { get; set; }
}

public class UpdatePerfilDto
{
    public string  Nombre           { get; set; } = string.Empty;
    public string? Email            { get; set; }
    public string? PasswordActual   { get; set; }
    public string? PasswordNueva    { get; set; }
    // Contraseña de correo saliente (smtp_password en configuracion)
    public string? SmtpPasswordNueva { get; set; }
}
