namespace CellApp.Services;

/// <summary>
/// Estado compartido entre componentes dentro del mismo circuito Blazor.
/// Permite que el dashboard informe al layout del conteo de alertas de stock
/// sin hacer una segunda llamada a la API.
/// </summary>
public class AppStateService
{
    public int  StockAlertCount { get; private set; }
    public bool DashboardCargado { get; private set; }

    public event Action? OnChange;

    public void SetStockAlertCount(int count)
    {
        StockAlertCount  = count;
        DashboardCargado = true;
        OnChange?.Invoke();
    }
}
