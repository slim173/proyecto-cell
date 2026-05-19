using CellApi.Repositories;
using System.Net.Http.Headers;
using System.Text;

namespace CellApi.Services;

public interface IWhatsAppService
{
    Task<bool> SendAsync(string telefono, string mensaje);
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

            var sid   = cfg.GetValueOrDefault("twilio_account_sid", "");
            var token = cfg.GetValueOrDefault("twilio_auth_token",  "");
            var from  = cfg.GetValueOrDefault("twilio_whatsapp_from", "whatsapp:+14155238886");

            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(token))
                return false;

            // Normalizar teléfono: añadir prefijo país si no lo tiene
            var to = telefono.StartsWith("+") ? telefono : "+34" + telefono.TrimStart('0');
            to = "whatsapp:" + to;

            var url = $"https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json";
            var body = new FormUrlEncodedContent(new Dictionary<string, string> {
                ["From"] = from,
                ["To"]   = to,
                ["Body"] = mensaje
            });

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{sid}:{token}")));

            var resp = await _http.PostAsync(url, body);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                _log.LogWarning("WhatsApp error {Status}: {Error}", resp.StatusCode, err);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error enviando WhatsApp a {Telefono}", telefono);
            return false;
        }
    }
}
