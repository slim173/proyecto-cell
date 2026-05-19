using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class ProductoApiService
{
    private readonly HttpClient _http;
    public ProductoApiService(HttpClient http) => _http = http;

    public async Task<List<ProductoDto>> GetAllAsync(bool soloActivos = true)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<ProductoDto>>>($"api/productos?soloActivos={soloActivos}");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<ProductoDto?> GetByIdAsync(int id)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<ProductoDto>>($"api/productos/{id}");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<List<CategoriaDto>> GetCategoriasAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<CategoriaDto>>>("api/productos/categorias");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<ProductoDto>> GetStockBajoAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<ProductoDto>>>("api/productos/stock-bajo");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<(bool ok, string msg)> CreateAsync(CreateProductoDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/productos", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<ProductoDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<(bool ok, string msg)> UpdateAsync(int id, UpdateProductoDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/productos/{id}", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<ProductoDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<(bool ok, string msg)> AjustarStockAsync(AjusteStockDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/productos/ajuste-stock", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<ProductoDto?> BuscarPorCodigoAsync(string q)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<ProductoDto>>(
                $"api/productos/buscar?q={Uri.EscapeDataString(q)}");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<(bool ok, string msg)> DeleteAsync(int id)
    {
        var resp = await _http.DeleteAsync($"api/productos/{id}");
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }
}
