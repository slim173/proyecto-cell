using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class UsuarioApiService
{
    private readonly HttpClient _http;
    public UsuarioApiService(HttpClient http) => _http = http;

    public async Task<PerfilDto?> GetPerfilAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<PerfilDto>>("api/usuarios/perfil");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<(bool ok, string msg)> UpdatePerfilAsync(UpdatePerfilDto dto)
    {
        var resp = await _http.PutAsJsonAsync("api/usuarios/perfil", dto);
        var r = await resp.Content.ReadFromJsonAsync<ApiResponse<PerfilDto>>();
        return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
    }

    // ── Admin ────────────────────────────────────────────────────────────────

    public async Task<List<UsuarioAdminDto>> GetAllAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<List<UsuarioAdminDto>>>("api/usuarios");
            return r?.Data ?? [];
        }
        catch { return []; }
    }

    public async Task<(bool ok, string msg)> CreateAsync(CreateUsuarioDto dto)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/usuarios", dto);
            var r = await resp.Content.ReadFromJsonAsync<ApiResponse<UsuarioAdminDto>>();
            return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string msg)> UpdateAsync(int id, UpdateUsuarioAdminDto dto)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync($"api/usuarios/{id}", dto);
            var r = await resp.Content.ReadFromJsonAsync<ApiResponse<UsuarioAdminDto>>();
            return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }
}
