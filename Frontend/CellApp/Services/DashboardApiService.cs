using CellApp.Models;
using System.Net.Http.Json;

namespace CellApp.Services;

public class DashboardApiService
{
    private readonly HttpClient _http;
    public DashboardApiService(HttpClient http) => _http = http;

    public async Task<DashboardDto?> GetAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<ApiResponse<DashboardDto>>("api/dashboard");
            return r?.Data;
        }
        catch { return null; }
    }
}
