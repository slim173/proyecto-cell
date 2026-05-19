using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class CompraApiService
{
    private readonly HttpClient _http;
    public CompraApiService(HttpClient http) => _http = http;

    public async Task<List<CompraDto>> GetAllAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<CompraDto>>>("api/compras");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<CompraDto?> GetByIdAsync(int id)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<CompraDto>>($"api/compras/{id}");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<(bool ok, string msg)> CreateAsync(CreateCompraDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/compras", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<CompraDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<List<ProveedorDto>> GetProveedoresAsync(bool soloActivos = true)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<ProveedorDto>>>($"api/compras/proveedores?soloActivos={soloActivos}");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<(bool ok, string msg)> CreateProveedorAsync(CreateProveedorDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/compras/proveedores", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<ProveedorDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<(bool ok, string msg)> EnviarPdfAsync(int id, string destinatario)
    {
        var resp = await _http.PostAsJsonAsync($"api/compras/{id}/enviar-pdf",
            new { Destinatario = destinatario });
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }
}
