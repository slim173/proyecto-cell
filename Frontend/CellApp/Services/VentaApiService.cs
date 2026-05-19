using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class VentaApiService
{
    private readonly HttpClient _http;
    public VentaApiService(HttpClient http) => _http = http;

    public async Task<List<VentaDto>> GetAllAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<VentaDto>>>("api/ventas");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<VentaDto?> GetByIdAsync(int id)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<VentaDto>>($"api/ventas/{id}");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<(bool ok, string msg, VentaDto? data)> CreateAsync(CreateVentaDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/ventas", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<VentaDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error", r?.Data);
    }

    public async Task<(bool ok, string msg)> UpdateEstadoAsync(int id, string estado)
    {
        var resp = await _http.PatchAsJsonAsync($"api/ventas/{id}/estado", new { Estado = estado });
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool ok, string msg)> GenerarFacturaAsync(int id)
    {
        var resp = await _http.PostAsync($"api/ventas/{id}/generar-factura", null);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<(bool ok, string msg)> EnviarFacturaAsync(int id)
    {
        var resp = await _http.PostAsync($"api/ventas/{id}/enviar-factura", null);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<(bool ok, string msg)> EnviarTicketAsync(int id, string destinatario)
    {
        var resp = await _http.PostAsJsonAsync($"api/ventas/{id}/enviar-pdf",
            new { Destinatario = destinatario });
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }
}
