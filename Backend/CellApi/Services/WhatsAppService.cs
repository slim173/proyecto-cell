using CellApi.Repositories;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CellApi.Services;

public interface IWhatsAppService
{
    Task<bool> SendAsync(string telefono, string mensaje);
    Task<bool> SendTemplateAsync(string telefono, string contentSid, Dictionary<string, string> variables);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly IConfiguracionRepository _config;
    private readonly HttpClient               _http;
    private readonly ILogger<WhatsAppService> _log;

    public WhatsAppService(IConfiguracionRepository config, IHttpClientFactory factory,
        ILogger<WhatsAppService> log)
    {
        _config = config;
        _http   = factory.CreateClient("twilio");
        _log    = log;
    }

    public async Task<bool> SendAsync(string telefono, string mensaje)
    {
        try
        {
            var cfg = await _config.GetAllAsync();

            if (!bool.TryParse(cfg.GetValueOrDefault("whatsapp_activo", "false"), out var activo) || !activo)
                return false;

            var sid        = cfg.GetValueOrDefault("twilio_account_sid",    "");
            var token      = cfg.GetValueOrDefault("twilio_auth_token",     "");
            var from       = cfg.GetValueOrDefault("twilio_whatsapp_from",  "whatsapp:+14155238886");
            var contentSid = cfg.GetValueOrDefault("twilio_content_sid",    "");

            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(token))
                return false;

            // Cuando hay ContentSid configurado, enviar como plantilla aprobada de WhatsApp.
            // Variable "1" recibe el texto completo del mensaje.
            if (!string.IsNullOrEmpty(contentSid))
            {
                var vars = new Dictionary<string, string> { ["1"] = mensaje };
                return await SendTemplateAsync(telefono, contentSid, vars, sid, token, from);
            }

            // Fallback: texto libre (funciona en sandbox Twilio y con números opt-in)
            return await SendBodyAsync(telefono, mensaje, sid, token, from);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error enviando WhatsApp a {Telefono}", telefono);
            return false;
        }
    }

    public Task<bool> SendTemplateAsync(string telefono, string contentSid, Dictionary<string, string> variables)
        => SendTemplateAsync(telefono, contentSid, variables, null, null, null);

    // ── Privado ────────────────────────────────────────────────────────

    private async Task<bool> SendTemplateAsync(
        string telefono, string contentSid, Dictionary<string, string> variables,
        string? sid, string? token, string? from)
    {
        try
        {
            if (sid == null || token == null || from == null)
            {
                var cfg = await _config.GetAllAsync();
                sid   = cfg.GetValueOrDefault("twilio_account_sid",   "");
                token = cfg.GetValueOrDefault("twilio_auth_token",    "");
                from  = cfg.GetValueOrDefault("twilio_whatsapp_from", "whatsapp:+14155238886");
            }

            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(token))
                return false;

            var to  = NormalizarTelefono(telefono);
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json";

            var fields = new Dictionary<string, string>
            {
                ["From"]             = from,
                ["To"]               = to,
                ["ContentSid"]       = contentSid,
                ["ContentVariables"] = JsonSerializer.Serialize(variables),
            };

            return await PostToTwilio(url, fields, sid, token);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error enviando plantilla WhatsApp a {Telefono}", telefono);
            return false;
        }
    }

    private async Task<bool> SendBodyAsync(string telefono, string mensaje, string sid, string token, string from)
    {
        var to  = NormalizarTelefono(telefono);
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json";

        var fields = new Dictionary<string, string>
        {
            ["From"] = from,
            ["To"]   = to,
            ["Body"] = mensaje,
        };

        return await PostToTwilio(url, fields, sid, token);
    }

    private async Task<bool> PostToTwilio(string url, Dictionary<string, string> fields, string sid, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{sid}:{token}")))
            },
            Content = new FormUrlEncodedContent(fields)
        };

        var resp = await _http.SendAsync(request);
        if (resp.IsSuccessStatusCode) return true;

        var err = await resp.Content.ReadAsStringAsync();
        _log.LogWarning("Twilio {Status}: {Error}", resp.StatusCode, err);
        return false;
    }

    private static string NormalizarTelefono(string telefono)
    {
        var t = telefono.StartsWith("+") ? telefono : "+34" + telefono.TrimStart('0');
        return t.StartsWith("whatsapp:") ? t : "whatsapp:" + t;
    }
}
