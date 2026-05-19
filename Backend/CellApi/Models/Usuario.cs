namespace CellApi.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "empleado";
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
}
