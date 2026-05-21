using CellApp.Handlers;
using CellApp.Services;

// Fijar directorio de trabajo al del exe (necesario para Windows Service)
Environment.CurrentDirectory = AppContext.BaseDirectory;

var builder = WebApplication.CreateBuilder(args);
if (OperatingSystem.IsWindows())
    builder.Host.UseWindowsService();

// ── Blazor Server ────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Auth State (scoped por circuit Blazor) ───────────────────────
builder.Services.AddScoped<AuthStateService>();

// ── Auth Header Handler (agrega JWT a peticiones HTTP) ───────────
builder.Services.AddScoped<AuthHeaderHandler>();

// ── HttpClient con handler de autenticación ──────────────────────
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";
builder.Services.AddScoped(sp =>
{
    var authHandler     = sp.GetRequiredService<AuthHeaderHandler>();
    authHandler.InnerHandler = new HttpClientHandler();
    return new HttpClient(authHandler)
    {
        BaseAddress = new Uri(apiBase)
    };
});

// ── Servicios de la aplicación ───────────────────────────────────
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<ClienteApiService>();
builder.Services.AddScoped<ProductoApiService>();
builder.Services.AddScoped<VentaApiService>();
builder.Services.AddScoped<ReparacionApiService>();
builder.Services.AddScoped<CompraApiService>();
builder.Services.AddScoped<InventarioApiService>();
builder.Services.AddScoped<FacturaApiService>();
builder.Services.AddScoped<DashboardApiService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddScoped<UsuarioApiService>();
builder.Services.AddScoped<EmpresaApiService>();
builder.Services.AddScoped<GarantiaApiService>();
builder.Services.AddScoped<CajaApiService>();
builder.Services.AddScoped<AppStateService>();
builder.Services.AddScoped<WhatsAppApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<CellApp.Components.App>()
    .AddInteractiveServerRenderMode();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var url  = $"http://+:{port}";
Console.WriteLine($"[Startup] PORT={port}, binding to {url}");

try
{
    app.Run(url);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[FATAL] {ex}");
    throw;
}
