using System.Net.Http.Json;
using CellApp.Models;

namespace CellApp.Services;

public class CajaApiService
{
    private readonly HttpClient _http;
    public CajaApiService(HttpClient http) => _http = http;

    public async Task<CajaSesionDto?> GetSesionActualAsync()
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<CajaSesionDto>>("api/caja/sesion-actual");
        return resp?.Data;
    }

    public async Task<List<CajaSesionDto>> GetHistorialAsync(int limit = 30)
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<List<CajaSesionDto>>>(
            $"api/caja/historial?limit={limit}");
        return resp?.Data ?? new();
    }

    public async Task<CajaSesionDto?> GetByIdAsync(int id)
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<CajaSesionDto>>($"api/caja/{id}");
        return resp?.Data;
    }

    public async Task<(bool ok, string msg, CajaSesionDto? sesion)> AbrirAsync(decimal efectivo, string? obs = null)
    {
        var resp = await _http.PostAsJsonAsync("api/caja/abrir",
            new { EfectivoApertura = efectivo, Observaciones = obs });
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<CajaSesionDto>>();
        return (resp.IsSuccessStatusCode, body?.Message ?? "", body?.Data);
    }

    public async Task<(bool ok, string msg)> CerrarAsync(int id, decimal efectivoCierre, string? obs = null)
    {
        var resp = await _http.PostAsJsonAsync($"api/caja/{id}/cerrar",
            new { EfectivoCierre = efectivoCierre, Observaciones = obs });
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (resp.IsSuccessStatusCode, body?.Message ?? "");
    }

    public async Task<(bool ok, string msg)> AddMovimientoAsync(int id, string tipo,
        string concepto, decimal importe, string? metodoPago = null)
    {
        var resp = await _http.PostAsJsonAsync($"api/caja/{id}/movimientos",
            new { Tipo = tipo, Concepto = concepto, Importe = importe, MetodoPago = metodoPago });
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (resp.IsSuccessStatusCode, body?.Message ?? "");
    }
}
