using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class InventarioApiService
{
    private readonly HttpClient _http;
    public InventarioApiService(HttpClient http) => _http = http;

    public async Task<List<InventarioMovimientoDto>> GetKardexAsync(
        int? productoId = null, DateTime? desde = null, DateTime? hasta = null)
    {
        try
        {
            var query = new List<string>();
            if (productoId.HasValue) query.Add($"productoId={productoId}");
            if (desde.HasValue)     query.Add($"desde={desde:yyyy-MM-ddTHH:mm:ss}");
            if (hasta.HasValue)     query.Add($"hasta={hasta:yyyy-MM-ddTHH:mm:ss}");

            var url = "api/inventario/kardex" + (query.Any() ? "?" + string.Join("&", query) : "");
            var r = await _http.GetFromJsonAsync<ApiResponse<List<InventarioMovimientoDto>>>(url);
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<ProductoStockBajoDto>> GetStockBajoAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<ProductoStockBajoDto>>>("api/inventario/stock-bajo");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }
}
