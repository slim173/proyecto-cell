namespace CellApp.Services;

public class ToastMessage
{
    public string Mensaje { get; set; } = string.Empty;
    public string Tipo    { get; set; } = "info"; // success | error | warning | info
}

public class NotificacionService
{
    public List<ToastMessage> Toasts { get; } = new();
    public event Action? OnChange;

    public void Success(string mensaje) => Add(mensaje, "success");
    public void Error(string mensaje)   => Add(mensaje, "error");
    public void Warning(string mensaje) => Add(mensaje, "warning");
    public void Info(string mensaje)    => Add(mensaje, "info");

    void Add(string mensaje, string tipo)
    {
        var toast = new ToastMessage { Mensaje = mensaje, Tipo = tipo };
        Toasts.Add(toast);
        OnChange?.Invoke();
        _ = RemoveAfterDelay(toast);
    }

    async Task RemoveAfterDelay(ToastMessage toast)
    {
        await Task.Delay(4500);
        Remove(toast);
    }

    public void Remove(ToastMessage toast)
    {
        Toasts.Remove(toast);
        OnChange?.Invoke();
    }
}
