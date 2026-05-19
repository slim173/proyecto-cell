using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class AuthApiService
{
    private readonly HttpClient _http;
    public AuthApiService(HttpClient http) => _http = http;

    public async Task<(bool ok, string msg, AuthResponseDto? data)> LoginAsync(LoginDto dto)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/auth/login", dto);
            var r    = await resp.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            return (r?.Success ?? false, r?.Message ?? r?.Errors.FirstOrDefault() ?? "Error", r?.Data);
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión: {ex.Message}", null);
        }
    }
}
