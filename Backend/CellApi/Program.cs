using System.Text;
using CellApi.Data;
using CellApi.Mappings;
using CellApi.Repositories;
using CellApi.Services;
using CellApi.Settings;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

// Fijar directorio de trabajo al del exe (necesario para Windows Service)
Environment.CurrentDirectory = AppContext.BaseDirectory;

// ── Garantizar que wwwroot existe ANTES de builder.Build() ──────
var webRootPreBuild = Path.Combine(AppContext.BaseDirectory, "wwwroot");
Directory.CreateDirectory(webRootPreBuild);

Console.WriteLine("[Startup] Iniciando CellApi...");

var builder = WebApplication.CreateBuilder(args);
if (OperatingSystem.IsWindows())
    builder.Host.UseWindowsService();
builder.WebHost.UseWebRoot(webRootPreBuild);   // garantiza WebRootPath != null

// ── DataProtection: usar /tmp para evitar cuelgue en containers ─
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/tmp/dp-keys"));

// ── Dapper: mapeo automático snake_case → PascalCase ────────────
DefaultTypeMap.MatchNamesWithUnderscores = true;

// ── QuestPDF: licencia Community (gratuita) ──────────────────────
QuestPDF.Settings.License = LicenseType.Community;

// ── JWT Authentication ───────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Controllers + Swagger ────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "CellShop ERP API",
        Version     = "v1",
        Description = "API REST para el sistema ERP de tienda de reparación de celulares (España)"
    });

    // JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Ingresa el token JWT. Ejemplo: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS (permite cualquier origen — JWT protege los endpoints) ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── Configuración tipada ─────────────────────────────────────────
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// ── Infraestructura de datos ─────────────────────────────────────
builder.Services.AddSingleton<DbConnectionFactory>();

// ── AutoMapper ───────────────────────────────────────────────────
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// ── Repositories ─────────────────────────────────────────────────
builder.Services.AddScoped<IClienteRepository,       ClienteRepository>();
builder.Services.AddScoped<IProductoRepository,      ProductoRepository>();
builder.Services.AddScoped<IVentaRepository,         VentaRepository>();
builder.Services.AddScoped<IReparacionRepository,    ReparacionRepository>();
builder.Services.AddScoped<ICompraRepository,        CompraRepository>();
builder.Services.AddScoped<IInventarioRepository,    InventarioRepository>();
builder.Services.AddScoped<IFacturaRepository,       FacturaRepository>();
builder.Services.AddScoped<IEmailLogRepository,      EmailLogRepository>();
builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
builder.Services.AddScoped<IUsuarioRepository,       UsuarioRepository>();
builder.Services.AddScoped<IGarantiaRepository,      GarantiaRepository>();
builder.Services.AddScoped<ICajaRepository,          CajaRepository>();

// ── Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IClienteService,    ClienteService>();
builder.Services.AddScoped<IProductoService,   ProductoService>();
builder.Services.AddScoped<IVentaService,      VentaService>();
builder.Services.AddScoped<IReparacionService, ReparacionService>();
builder.Services.AddScoped<ICompraService,     CompraService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IFacturaService,    FacturaService>();
builder.Services.AddScoped<IEmailService,      EmailService>();
builder.Services.AddScoped<IPdfService,        PdfService>();
builder.Services.AddScoped<IDashboardService,  DashboardService>();
builder.Services.AddScoped<IAuthService,       AuthService>();
builder.Services.AddScoped<IWhatsAppService,   WhatsAppService>();

// ── HttpClient para Twilio (WhatsApp) ────────────────────────────
builder.Services.AddHttpClient("twilio");

// ── Hosted Services ──────────────────────────────────────────────
builder.Services.AddHostedService<RecordatorioHostedService>();

// ── Logging ──────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// ── Crear directorios necesarios ─────────────────────────────────
var wwwroot = app.Environment.WebRootPath!;  // garantizado no-null gracias a UseWebRoot()
Directory.CreateDirectory(Path.Combine(wwwroot, "facturas"));
Directory.CreateDirectory(Path.Combine(wwwroot, "reparaciones"));
Directory.CreateDirectory(Path.Combine(wwwroot, "logos"));

// ── Middleware pipeline ──────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CellShop ERP API v1");
        c.RoutePrefix = "swagger";
    });
}

// ── Migraciones automáticas de BD ───────────────────────────────
await DbMigrator.RunAsync(app.Services.GetRequiredService<DbConnectionFactory>());

app.UseCors("AllowAll");

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[FATAL] {ex}");
    throw;
}
