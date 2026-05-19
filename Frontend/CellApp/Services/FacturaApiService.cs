using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class FacturaApiService
{
    private readonly HttpClient _http;
    public FacturaApiService(HttpClient http) => _http = http;

    public async Task<List<FacturaDto>> GetAllAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<FacturaDto>>>("api/facturas");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<FacturaDto?> GetByIdAsync(int id)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<FacturaDto>>($"api/facturas/{id}");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<byte[]?> DescargarPdfAsync(int id)
    {
        try { return await _http.GetByteArrayAsync($"api/facturas/{id}/pdf"); }
        catch { return null; }
    }

    public async Task<(bool ok, string msg)> AnularAsync(int id, string motivo)
    {
        var resp = await _http.PostAsJsonAsync($"api/facturas/{id}/anular", new { MotivoAnulacion = motivo });
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool ok, int id, string msg)> CreateAsync(CreateFacturaDto dto)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/facturas", dto);
            var r = await resp.Content.ReadFromJsonAsync<ApiResponse<CrearFacturaResponse>>();
            if (r?.Success == true && r.Data != null)
                return (true, r.Data.Id, r.Message);
            return (false, 0, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error al crear la factura.");
        }
        catch (Exception ex) { return (false, 0, ex.Message); }
    }

    public async Task<(bool ok, string msg)> EnviarEmailAsync(int id, string destinatario)
    {
        var resp = await _http.PostAsJsonAsync($"api/facturas/{id}/enviar-email",
            new { Destinatario = destinatario });
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }
}
