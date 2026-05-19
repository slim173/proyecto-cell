using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class EmpresaApiService
{
    private readonly HttpClient _http;
    public EmpresaApiService(HttpClient http) => _http = http;

    public async Task<EmpresaDto?> GetAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<EmpresaDto>>("api/configuracion/empresa");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<(bool ok, string msg)> UpdateAsync(UpdateEmpresaDto dto)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync("api/configuracion/empresa", dto);
            var r    = await resp.Content.ReadFromJsonAsync<ApiResponse<EmpresaDto>>();
            return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task<(bool ok, string msg, string? logoUrl)> SubirLogoAsync(Stream stream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var sc      = new StreamContent(stream);
            content.Add(sc, "archivo", fileName);

            var resp = await _http.PostAsync("api/configuracion/logo", content);
            var r    = await resp.Content.ReadFromJsonAsync<ApiResponse<string>>();
            return (r?.Success ?? false, r?.Message ?? "Error", r?.Data);
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message, null);
        }
    }

    public async Task<(bool ok, string msg)> EliminarLogoAsync()
    {
        try
        {
            var resp = await _http.DeleteAsync("api/configuracion/logo");
            var r    = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
            return (r?.Success ?? false, r?.Message ?? "Error");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }
}
