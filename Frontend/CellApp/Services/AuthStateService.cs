namespace CellApp.Services;

public class AuthStateService
{
    public bool    IsAuthenticated { get; private set; }
    public string? Username        { get; private set; }
    public string? Nombre          { get; private set; }
    public string? Token           { get; private set; }
    public string? Rol             { get; private set; }

    public bool IsAdmin => Rol == "admin";

    public event Action? OnChange;

    public void SetUser(string username, string nombre, string token, string rol = "empleado")
    {
        IsAuthenticated = true;
        Username        = username;
        Nombre          = nombre;
        Token           = token;
        Rol             = rol;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        IsAuthenticated = false;
        Username        = null;
        Nombre          = null;
        Token           = null;
        Rol             = null;
        OnChange?.Invoke();
    }
}
