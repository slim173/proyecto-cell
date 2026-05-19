using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class WhatsAppApiService
{
    private readonly HttpClient _http;

    public WhatsAppApiService(HttpClient http) => _http = http;

    public async Task<(bool ok, WhatsAppResultadoDto? resultado, string msg)> EnviarAsync(EnviarWhatsAppDto dto)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/whatsapp/enviar", dto);
            var api  = await resp.Content.ReadFromJsonAsync<ApiResponse<WhatsAppResultadoDto>>();
            if (api == null) return (false, null, "Sin respuesta del servidor.");
            return (api.Success, api.Data, api.Message ?? "");
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool ok, List<WhatsAppResultadoDto> resultados, string msg)> EnviarMasivoAsync(EnviarWhatsAppMasivoDto dto)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/whatsapp/enviar-masivo", dto);
            var api  = await resp.Content.ReadFromJsonAsync<ApiResponse<List<WhatsAppResultadoDto>>>();
            if (api == null) return (false, new(), "Sin respuesta del servidor.");
            return (api.Success, api.Data ?? new(), api.Message ?? "");
        }
        catch (Exception ex) { return (false, new(), ex.Message); }
    }

    public async Task<List<WhatsAppLogDto>> GetHistorialAsync(int limit = 100)
    {
        try
        {
            var api = await _http.GetFromJsonAsync<ApiResponse<List<WhatsAppLogDto>>>(
                $"api/whatsapp/historial?limit={limit}");
            return api?.Data ?? new();
        }
        catch { return new(); }
    }
}
