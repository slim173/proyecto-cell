using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class ReparacionApiService
{
    private readonly HttpClient _http;
    public ReparacionApiService(HttpClient http) => _http = http;

    public async Task<List<ReparacionDto>> GetAllAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<ReparacionDto>>>("api/reparaciones");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<ReparacionDto?> GetByIdAsync(int id)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<ReparacionDto>>($"api/reparaciones/{id}");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<(bool ok, string msg, ReparacionDto? data)> CreateAsync(CreateReparacionDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/reparaciones", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<ReparacionDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error", r?.Data);
    }

    public async Task<(bool ok, string msg, ReparacionDto? data)> UpdateAsync(int id, UpdateReparacionDto dto)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync($"api/reparaciones/{id}", dto);
            var r = await resp.Content.ReadFromJsonAsync<ApiResponse<ReparacionDto>>();
            return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error", r?.Data);
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    public async Task<(bool ok, string msg)> UpdateEstadoAsync(int id, UpdateReparacionEstadoDto dto)
    {
        var resp = await _http.PatchAsJsonAsync($"api/reparaciones/{id}/estado", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<ReparacionDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<(bool ok, string msg)> AddDetalleAsync(int id, AddReparacionDetalleDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/reparaciones/{id}/detalles", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<(bool ok, string msg)> RemoveDetalleAsync(int id, int detalleId)
    {
        var resp = await _http.DeleteAsync($"api/reparaciones/{id}/detalles/{detalleId}");
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool ok, string msg)> NotificarAsync(int id)
    {
        var resp = await _http.PostAsync($"api/reparaciones/{id}/notificar", null);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool ok, string msg)> EnviarPdfAsync(int id, string destinatario)
    {
        var resp = await _http.PostAsJsonAsync($"api/reparaciones/{id}/enviar-pdf",
            new { Destinatario = destinatario });
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    public async Task<List<HistorialEquipoDto>> GetHistorialEquipoAsync(int id)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<HistorialEquipoDto>>>(
                $"api/reparaciones/{id}/historial-equipo");
            return r?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<(bool ok, string msg)> EliminarImagenAsync(int reparacionId, int imagenId)
    {
        try
        {
            var resp = await _http.DeleteAsync($"api/reparaciones/{reparacionId}/imagenes/{imagenId}");
            var r = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
            return (r?.Success ?? false, r?.Message ?? "Error");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<int?> GetFacturaIdAsync(int reparacionId)
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<int?>>($"api/reparaciones/{reparacionId}/factura-id");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<(bool ok, string msg, List<ReparacionImagenDto> imagenes)> SubirImagenesAsync(
        int id, IEnumerable<(string fileName, byte[] data)> archivos)
    {
        using var form = new MultipartFormDataContent();
        foreach (var (fileName, data) in archivos)
        {
            var fileContent = new ByteArrayContent(data);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(fileContent, "archivos", fileName);
        }
        var resp = await _http.PostAsync($"api/reparaciones/{id}/imagenes", form);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<List<ReparacionImagenDto>>>();
        return (r?.Success ?? false,
                r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error",
                r?.Data ?? new());
    }
}
