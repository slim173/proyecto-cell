using System.Net.Http.Json;
using CellApp.Models;

namespace CellApp.Services;

public class GarantiaApiService
{
    private readonly HttpClient _http;
    public GarantiaApiService(HttpClient http) => _http = http;

    public async Task<List<GarantiaDto>> GetAllAsync(int? clienteId = null)
    {
        var url = clienteId.HasValue
            ? $"api/garantias?clienteId={clienteId}"
            : "api/garantias";
        var resp = await _http.GetFromJsonAsync<ApiResponse<List<GarantiaDto>>>(url);
        return resp?.Data ?? new();
    }

    public async Task<GarantiaDto?> GetByIdAsync(int id)
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<GarantiaDto>>($"api/garantias/{id}");
        return resp?.Data;
    }

    public async Task<(bool ok, string msg)> CreateAsync(CreateGarantiaDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/garantias", dto);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<GarantiaDto>>();
        return (resp.IsSuccessStatusCode, body?.Message ?? "Error");
    }

    public async Task<bool> UpdateEstadoAsync(int id, string estado)
    {
        var resp = await _http.PatchAsJsonAsync($"api/garantias/{id}/estado",
            new { Estado = estado });
        return resp.IsSuccessStatusCode;
    }
}
