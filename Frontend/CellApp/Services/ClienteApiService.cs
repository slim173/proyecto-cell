using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class ClienteApiService
{
    private readonly HttpClient _http;
    public ClienteApiService(HttpClient http) => _http = http;

    public async Task<List<ClienteDto>> GetAllAsync(bool soloActivos = true)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<ClienteDto>>>($"api/clientes?soloActivos={soloActivos}");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<ClienteDto?> GetByIdAsync(int id)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<ClienteDto>>($"api/clientes/{id}");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<(bool ok, string msg)> CreateAsync(CreateClienteDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/clientes", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<ClienteDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<(bool ok, string msg)> UpdateAsync(int id, UpdateClienteDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/clientes/{id}", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<ClienteDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<(bool ok, string msg)> DeleteAsync(int id)
    {
        var resp = await _http.DeleteAsync($"api/clientes/{id}");
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }
}
