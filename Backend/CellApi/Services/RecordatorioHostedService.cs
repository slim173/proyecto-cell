using CellApi.Repositories;

namespace CellApi.Services;

public class RecordatorioHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<RecordatorioHostedService> _log;

    public RecordatorioHostedService(IServiceScopeFactory scopes,
        ILogger<RecordatorioHostedService> log)
    {
        _scopes = scopes;
        _log    = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcesarRecordatorios();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error en RecordatorioHostedService");
            }
            // Revisar cada 6 horas
            await Task.Delay(TimeSpan.FromHours(6), ct);
        }
    }

    private async Task ProcesarRecordatorios()
    {
        using var scope = _scopes.CreateScope();
        var repo    = scope.ServiceProvider.GetRequiredService<IReparacionRepository>();
        var email   = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var wa      = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
        var config  = scope.ServiceProvider.GetRequiredService<IConfiguracionRepository>();
        var cliente = scope.ServiceProvider.GetRequiredService<IClienteRepository>();

        var cfg = await config.GetAllAsync();
        if (!bool.TryParse(cfg.GetValueOrDefault("recordatorio_activo", "true"), out var activo) || !activo)
            return;

        if (!int.TryParse(cfg.GetValueOrDefault("recordatorio_dias", "3"), out var dias))
            dias = 3;

        var empresa = cfg.GetValueOrDefault("empresa_nombre", "CellShop");
        var tel     = cfg.GetValueOrDefault("empresa_telefono", "");

        var pendientes = await repo.GetReparadasSinRecogerAsync(dias);
        foreach (var rep in pendientes)
        {
            try
            {
                var cl = await cliente.GetByIdAsync(rep.ClienteId);
                if (cl == null) continue;

                var asunto = $"Su reparación {rep.NumeroOrden} está lista para recoger — {empresa}";
                var cuerpo = $@"
                    <h2>Su dispositivo está listo, {cl.Nombre}</h2>
                    <p>Su reparación <strong>{rep.NumeroOrden}</strong> ha sido completada y está
                    esperando ser recogida en nuestra tienda.</p>
                    <p><strong>Dispositivo:</strong> {rep.Dispositivo} {rep.Marca} {rep.Modelo}</p>
                    {(rep.Total.HasValue ? $"<p><strong>Importe a pagar:</strong> {rep.Total:F2} €</p>" : "")}
                    <p>Nuestro horario: lunes a viernes 10:00–14:00 y 16:00–20:00</p>
                    {(string.IsNullOrEmpty(tel) ? "" : $"<p>Teléfono: {tel}</p>")}
                    <p>Un saludo,<br/>{empresa}</p>";

                if (!string.IsNullOrEmpty(cl.Email))
                    await email.SendAsync(cl.Email, asunto, cuerpo,
                        "recordatorio_recogida", "reparacion", rep.Id);

                var importeWa = rep.Total.HasValue      ? $". Total: {rep.Total:F2}€"
                             : rep.PrecioEstimado.HasValue ? $". Presupuesto: {rep.PrecioEstimado:F2}€"
                             : "";
                var msg = $"Hola {cl.Nombre}, su {rep.Dispositivo} {rep.Marca} está listo para recoger " +
                          $"(Orden: {rep.NumeroOrden}){importeWa}." +
                          $" — {empresa}" +
                          (string.IsNullOrEmpty(tel) ? "" : $" {tel}");

                if (!string.IsNullOrEmpty(cl.Telefono))
                    await wa.SendAsync(cl.Telefono, msg);

                await repo.MarcarRecordatorioEnviadoAsync(rep.Id);
                _log.LogInformation("Recordatorio enviado para reparación {NumeroOrden}", rep.NumeroOrden);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Error enviando recordatorio para reparación {Id}", rep.Id);
            }
        }
    }
}
