using CellApi.DTOs;
using CellApi.Repositories;
using CellApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionRepository _repo;
    private readonly IWebHostEnvironment       _env;
    private readonly IPdfService               _pdf;

    public ConfiguracionController(IConfiguracionRepository repo, IWebHostEnvironment env, IPdfService pdf)
    {
        _repo = repo;
        _env  = env;
        _pdf  = pdf;
    }

    // Claves que se exponen (smtp_password NO se devuelve por seguridad)
    private static readonly string[] ClavesEmpresa =
    [
        "empresa_nombre", "empresa_cif", "empresa_direccion", "empresa_ciudad",
        "empresa_cp", "empresa_telefono", "empresa_email", "empresa_web",
        "factura_pie_texto", "iva_porcentaje",
        "smtp_host", "smtp_puerto", "smtp_ssl", "smtp_usuario",
        "smtp_from_name", "smtp_from_email",
        "wa_msg_entrada", "wa_msg_listo", "wa_msg_recordatorio",
        "whatsapp_activo", "twilio_account_sid", "twilio_whatsapp_from",
        "recordatorio_activo", "recordatorio_dias",
        "ticket_formato", "ticket_clausula_reparacion", "ticket_clausula_recogida", "ticket_mostrar_qr",
        "empresa_logo",
        "empresa_url_publica"
    ];

    [HttpGet("empresa")]
    public async Task<ActionResult<ApiResponse<EmpresaDto>>> GetEmpresa()
    {
        var cfg = await _repo.GetAllAsync();
        return Ok(ApiResponse<EmpresaDto>.Ok(MapToDto(cfg)));
    }

    [HttpPut("empresa")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<EmpresaDto>>> UpdateEmpresa([FromBody] UpdateEmpresaDto dto)
    {
        var valores = new Dictionary<string, string>
        {
            ["empresa_nombre"]    = dto.Nombre,
            ["empresa_cif"]       = dto.Cif         ?? "",
            ["empresa_direccion"] = dto.Direccion    ?? "",
            ["empresa_ciudad"]    = dto.Ciudad       ?? "",
            ["empresa_cp"]        = dto.CodigoPostal ?? "",
            ["empresa_telefono"]  = dto.Telefono     ?? "",
            ["empresa_email"]     = dto.Email        ?? "",
            ["empresa_web"]       = dto.Web          ?? "",
            ["factura_pie_texto"] = dto.PieFactura   ?? "",
            ["iva_porcentaje"]    = dto.IvaPorcentaje.ToString(),
            ["smtp_host"]         = dto.SmtpHost     ?? "",
            ["smtp_puerto"]       = dto.SmtpPuerto.ToString(),
            ["smtp_ssl"]          = dto.SmtpSsl.ToString().ToLower(),
            ["smtp_usuario"]      = dto.SmtpUsuario  ?? "",
            ["smtp_from_name"]    = dto.SmtpFromName  ?? dto.Nombre,
            ["smtp_from_email"]   = dto.SmtpFromEmail ?? dto.SmtpUsuario ?? "",
            ["wa_msg_entrada"]             = dto.WaMsgEntrada        ?? "",
            ["wa_msg_listo"]              = dto.WaMsgListo           ?? "",
            ["wa_msg_recordatorio"]       = dto.WaMsgRecordatorio    ?? "",
            ["whatsapp_activo"]           = dto.WhatsappActivo.ToString().ToLower(),
            ["twilio_account_sid"]        = dto.TwilioAccountSid     ?? "",
            ["twilio_whatsapp_from"]      = dto.TwilioWhatsappFrom   ?? "",
            ["recordatorio_activo"]       = dto.RecordatorioActivo.ToString().ToLower(),
            ["recordatorio_dias"]         = dto.RecordatorioDias.ToString(),
            ["ticket_formato"]            = dto.TicketFormato        ?? "a4",
            ["ticket_clausula_reparacion"]= dto.ClausulaReparacion   ?? "",
            ["ticket_clausula_recogida"]  = dto.ClausulaRecogida     ?? "",
            ["ticket_mostrar_qr"]         = dto.TicketMostrarQr.ToString().ToLower(),
            ["empresa_url_publica"]       = dto.UrlPublica       ?? "",
        };

        if (!string.IsNullOrWhiteSpace(dto.SmtpPassword))
            valores["smtp_password"] = dto.SmtpPassword;
        if (!string.IsNullOrWhiteSpace(dto.TwilioAuthToken))
            valores["twilio_auth_token"] = dto.TwilioAuthToken;

        await _repo.SetMultipleAsync(valores);
        var cfg = await _repo.GetAllAsync();
        return Ok(ApiResponse<EmpresaDto>.Ok(MapToDto(cfg), "Configuración guardada correctamente."));
    }

    // ── Logo ────────────────────────────────────────────────────────
    [HttpPost("logo")]
    [RequestSizeLimit(5_242_880)] // 5 MB máximo
    public async Task<ActionResult<ApiResponse<string>>> SubirLogo([FromForm] IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("No se recibió ningún archivo."));

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!permitidas.Contains(ext))
            return BadRequest(ApiResponse<string>.Fail("Formato no permitido. Use JPG, PNG o WebP."));

        // Siempre se sobreescribe con el mismo nombre para simplificar caché
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var logoDir = Path.Combine(webRoot, "logos");
        Directory.CreateDirectory(logoDir);

        // Borra logos anteriores de cualquier extensión
        foreach (var old in Directory.GetFiles(logoDir, "logo.*"))
            System.IO.File.Delete(old);

        var fileName   = $"logo{ext}";
        var rutaFisica = Path.Combine(logoDir, fileName);
        await using var stream = System.IO.File.Create(rutaFisica);
        await archivo.CopyToAsync(stream);

        var rutaRelativa = $"logos/{fileName}";
        await _repo.SetMultipleAsync(new Dictionary<string, string>
        {
            ["empresa_logo"] = rutaRelativa
        });

        return Ok(ApiResponse<string>.Ok($"/{rutaRelativa}", "Logo subido correctamente."));
    }

    [HttpDelete("logo")]
    public async Task<ActionResult<ApiResponse>> EliminarLogo()
    {
        var cfg      = await _repo.GetAllAsync();
        var logoPath = cfg.GetValueOrDefault("empresa_logo", "");
        if (!string.IsNullOrEmpty(logoPath))
        {
            var webRoot   = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var rutaFisica = Path.Combine(webRoot, logoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(rutaFisica))
                System.IO.File.Delete(rutaFisica);
        }

        await _repo.SetMultipleAsync(new Dictionary<string, string> { ["empresa_logo"] = "" });
        return Ok(ApiResponse.Ok("Logo eliminado correctamente."));
    }

    // ── Info pública de la empresa (sin datos sensibles) ─────────────
    [AllowAnonymous]
    [HttpGet("empresa-publica")]
    public async Task<IActionResult> EmpresaPublica()
    {
        var cfg = await _repo.GetAllAsync();
        return Ok(ApiResponse<object>.Ok(new
        {
            nombre   = cfg.GetValueOrDefault("empresa_nombre",   ""),
            telefono = cfg.GetValueOrDefault("empresa_telefono", ""),
            email    = cfg.GetValueOrDefault("empresa_email",    ""),
            web      = cfg.GetValueOrDefault("empresa_web",      ""),
            ciudad   = cfg.GetValueOrDefault("empresa_ciudad",   ""),
            logoUrl  = cfg.GetValueOrDefault("empresa_logo",     "")
        }));
    }

    // ── QR de contacto WhatsApp ─────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("qr-contacto")]
    public async Task<IActionResult> QrContacto()
    {
        var cfg = await _repo.GetAllAsync();
        var tel = cfg.GetValueOrDefault("empresa_telefono", "").Trim();
        if (string.IsNullOrEmpty(tel))
            return NotFound("No hay teléfono configurado.");

        // Construir URL WhatsApp (solo dígitos, sin espacios ni guiones)
        var telLimpio = new string(tel.Where(c => char.IsDigit(c) || c == '+').ToArray())
                            .TrimStart('+');
        if (telLimpio.Length < 7)
            return BadRequest("Número de teléfono demasiado corto.");

        var contenido = $"https://wa.me/{telLimpio}";
        var png = _pdf.GenerarQrPng(contenido, 400);

        Response.Headers.Append("Cache-Control", "public, max-age=300");
        return File(png, "image/png");
    }

    // ── Helper ──────────────────────────────────────────────────────
    private static EmpresaDto MapToDto(Dictionary<string, string> cfg)
    {
        var logoRel = cfg.GetValueOrDefault("empresa_logo", "");
        return new EmpresaDto
        {
            Nombre           = cfg.GetValueOrDefault("empresa_nombre",    "CellShop"),
            Cif              = cfg.GetValueOrDefault("empresa_cif",       ""),
            Direccion        = cfg.GetValueOrDefault("empresa_direccion", ""),
            Ciudad           = cfg.GetValueOrDefault("empresa_ciudad",    ""),
            CodigoPostal     = cfg.GetValueOrDefault("empresa_cp",        ""),
            Telefono         = cfg.GetValueOrDefault("empresa_telefono",  ""),
            Email            = cfg.GetValueOrDefault("empresa_email",     ""),
            Web              = cfg.GetValueOrDefault("empresa_web",       ""),
            PieFactura       = cfg.GetValueOrDefault("factura_pie_texto", ""),
            IvaPorcentaje    = decimal.TryParse(cfg.GetValueOrDefault("iva_porcentaje","21"), out var iva) ? iva : 21,
            SmtpHost         = cfg.GetValueOrDefault("smtp_host",         "smtp.gmail.com"),
            SmtpPuerto       = int.TryParse(cfg.GetValueOrDefault("smtp_puerto","587"), out var pt) ? pt : 587,
            SmtpSsl          = cfg.GetValueOrDefault("smtp_ssl","true").Equals("true", StringComparison.OrdinalIgnoreCase),
            SmtpUsuario      = cfg.GetValueOrDefault("smtp_usuario",      ""),
            SmtpFromName     = cfg.GetValueOrDefault("smtp_from_name",    "CellShop"),
            SmtpFromEmail    = cfg.GetValueOrDefault("smtp_from_email",   ""),
            TieneSmtpPassword= cfg.ContainsKey("smtp_password") && !string.IsNullOrWhiteSpace(cfg["smtp_password"]),
            WaMsgEntrada      = cfg.GetValueOrDefault("wa_msg_entrada",      "¡Hola {nombre}! Hemos recibido tu {dispositivo} en {tienda}. Orden #{orden}. Te avisaremos cuando esté listo. 🔧"),
            WaMsgListo        = cfg.GetValueOrDefault("wa_msg_listo",        "¡Hola {nombre}! ✅ Tu {dispositivo} ya está listo. Pasa a recogerlo cuando quieras. Orden #{orden} · Total: {total}€"),
            WaMsgRecordatorio = cfg.GetValueOrDefault("wa_msg_recordatorio", "Hola {nombre}, te recordamos que tienes el {dispositivo} (Orden #{orden}) pendiente de recoger en {tienda}. 📱"),
            WhatsappActivo       = cfg.GetValueOrDefault("whatsapp_activo","false").Equals("true", StringComparison.OrdinalIgnoreCase),
            TwilioAccountSid    = cfg.GetValueOrDefault("twilio_account_sid", ""),
            TieneTwilioAuthToken= cfg.ContainsKey("twilio_auth_token") && !string.IsNullOrWhiteSpace(cfg["twilio_auth_token"]),
            TwilioWhatsappFrom  = cfg.GetValueOrDefault("twilio_whatsapp_from", ""),
            RecordatorioActivo  = cfg.GetValueOrDefault("recordatorio_activo","false").Equals("true", StringComparison.OrdinalIgnoreCase),
            RecordatorioDias    = int.TryParse(cfg.GetValueOrDefault("recordatorio_dias","3"), out var rd) ? rd : 3,
            TicketFormato      = cfg.GetValueOrDefault("ticket_formato",               "a4"),
            ClausulaReparacion = cfg.GetValueOrDefault("ticket_clausula_reparacion",   ""),
            ClausulaRecogida   = cfg.GetValueOrDefault("ticket_clausula_recogida",     ""),
            TicketMostrarQr    = cfg.GetValueOrDefault("ticket_mostrar_qr","true").Equals("true", StringComparison.OrdinalIgnoreCase),
            LogoUrl            = string.IsNullOrEmpty(logoRel) ? null : $"/{logoRel}",
            UrlPublica         = cfg.GetValueOrDefault("empresa_url_publica", ""),
        };
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────
public class EmpresaDto
{
    public string  Nombre         { get; set; } = string.Empty;
    public string? Cif            { get; set; }
    public string? Direccion      { get; set; }
    public string? Ciudad         { get; set; }
    public string? CodigoPostal   { get; set; }
    public string? Telefono       { get; set; }
    public string? Email          { get; set; }
    public string? Web            { get; set; }
    public string? PieFactura     { get; set; }
    public decimal IvaPorcentaje  { get; set; } = 21;
    public string? SmtpHost       { get; set; }
    public int     SmtpPuerto     { get; set; } = 587;
    public bool    SmtpSsl        { get; set; } = true;
    public string? SmtpUsuario    { get; set; }
    public string? SmtpFromName   { get; set; }
    public string? SmtpFromEmail  { get; set; }
    public bool    TieneSmtpPassword { get; set; }
    public string? WaMsgEntrada      { get; set; }
    public string? WaMsgListo        { get; set; }
    public string? WaMsgRecordatorio { get; set; }
    public bool    WhatsappActivo       { get; set; }
    public string? TwilioAccountSid    { get; set; }
    public bool    TieneTwilioAuthToken { get; set; }
    public string? TwilioWhatsappFrom  { get; set; }
    public bool    RecordatorioActivo  { get; set; }
    public int     RecordatorioDias    { get; set; } = 3;
    public string  TicketFormato      { get; set; } = "a4";
    public string? ClausulaReparacion { get; set; }
    public string? ClausulaRecogida   { get; set; }
    public bool    TicketMostrarQr    { get; set; } = true;
    public string? LogoUrl            { get; set; }
    public string? UrlPublica         { get; set; }
}

public class UpdateEmpresaDto
{
    public string  Nombre         { get; set; } = string.Empty;
    public string? Cif            { get; set; }
    public string? Direccion      { get; set; }
    public string? Ciudad         { get; set; }
    public string? CodigoPostal   { get; set; }
    public string? Telefono       { get; set; }
    public string? Email          { get; set; }
    public string? Web            { get; set; }
    public string? PieFactura     { get; set; }
    public decimal IvaPorcentaje  { get; set; } = 21;
    public string? SmtpHost       { get; set; }
    public int     SmtpPuerto     { get; set; } = 587;
    public bool    SmtpSsl        { get; set; } = true;
    public string? SmtpUsuario    { get; set; }
    public string? SmtpPassword   { get; set; }
    public string? SmtpFromName   { get; set; }
    public string? SmtpFromEmail  { get; set; }
    public string? WaMsgEntrada      { get; set; }
    public string? WaMsgListo        { get; set; }
    public string? WaMsgRecordatorio { get; set; }
    public bool    WhatsappActivo       { get; set; }
    public string? TwilioAccountSid    { get; set; }
    public string? TwilioAuthToken     { get; set; }
    public string? TwilioWhatsappFrom  { get; set; }
    public bool    RecordatorioActivo  { get; set; }
    public int     RecordatorioDias    { get; set; } = 3;
    public string  TicketFormato      { get; set; } = "a4";
    public string? ClausulaReparacion { get; set; }
    public string? ClausulaRecogida   { get; set; }
    public bool    TicketMostrarQr    { get; set; } = true;
    public string? UrlPublica         { get; set; }
}
