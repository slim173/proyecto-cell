using CellApi.DTOs;
using CellApi.Models;
using CellApi.Repositories;
using CellApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppService      _wa;
    private readonly IClienteRepository   _clientes;
    private readonly IEmailLogRepository  _logs;

    public WhatsAppController(
        IWhatsAppService     wa,
        IClienteRepository   clientes,
        IEmailLogRepository  logs)
    {
        _wa       = wa;
        _clientes = clientes;
        _logs     = logs;
    }

    // POST api/whatsapp/enviar
    [HttpPost("enviar")]
    public async Task<ActionResult<ApiResponse<WhatsAppResultadoDto>>> Enviar(
        [FromBody] EnviarWhatsAppDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Mensaje))
            return BadRequest(ApiResponse<WhatsAppResultadoDto>.Fail("El mensaje no puede estar vacío."));

        var cliente = await _clientes.GetByIdAsync(dto.ClienteId);
        if (cliente == null)
            return NotFound(ApiResponse<WhatsAppResultadoDto>.Fail("Cliente no encontrado."));

        if (string.IsNullOrWhiteSpace(cliente.Telefono))
            return BadRequest(ApiResponse<WhatsAppResultadoDto>.Fail(
                $"{cliente.Nombre} no tiene número de teléfono registrado."));

        var ok = await _wa.SendAsync(cliente.Telefono, dto.Mensaje);

        var logId = await _logs.CreateAsync(new EmailLog
        {
            Destinatario   = cliente.Telefono,
            Asunto         = dto.Mensaje.Length > 100 ? dto.Mensaje[..97] + "…" : dto.Mensaje,
            Cuerpo         = dto.Mensaje,
            Tipo           = "whatsapp",
            ReferenciaTipo = "cliente",
            ReferenciaId   = cliente.Id,
            Estado         = ok ? "enviado" : "error",
            Intentos       = 1,
            FechaEnvio     = ok ? DateTime.UtcNow : null
        });

        if (!ok) await _logs.UpdateEstadoAsync(logId, "error",
            "WhatsApp no configurado o error de envío", null);

        var resultado = new WhatsAppResultadoDto
        {
            ClienteId     = cliente.Id,
            ClienteNombre = $"{cliente.Nombre} {cliente.Apellidos}".Trim(),
            Telefono      = cliente.Telefono,
            Ok            = ok,
            Mensaje       = ok ? "Mensaje enviado correctamente." : "No se pudo enviar (Twilio no configurado)."
        };

        return Ok(ApiResponse<WhatsAppResultadoDto>.Ok(resultado,
            ok ? $"Mensaje enviado a {cliente.Nombre}." : "Twilio no configurado — usa el enlace manual."));
    }

    // POST api/whatsapp/enviar-masivo
    [HttpPost("enviar-masivo")]
    public async Task<ActionResult<ApiResponse<List<WhatsAppResultadoDto>>>> EnviarMasivo(
        [FromBody] EnviarWhatsAppMasivoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Mensaje))
            return BadRequest(ApiResponse<List<WhatsAppResultadoDto>>.Fail("El mensaje no puede estar vacío."));
        if (dto.ClienteIds.Count == 0)
            return BadRequest(ApiResponse<List<WhatsAppResultadoDto>>.Fail("Selecciona al menos un destinatario."));

        var resultados = new List<WhatsAppResultadoDto>();

        foreach (var id in dto.ClienteIds)
        {
            var cliente = await _clientes.GetByIdAsync(id);
            if (cliente == null) continue;

            if (string.IsNullOrWhiteSpace(cliente.Telefono))
            {
                resultados.Add(new WhatsAppResultadoDto
                {
                    ClienteId     = id,
                    ClienteNombre = $"{cliente.Nombre} {cliente.Apellidos}".Trim(),
                    Telefono      = "",
                    Ok            = false,
                    Mensaje       = "Sin teléfono registrado."
                });
                continue;
            }

            var ok = await _wa.SendAsync(cliente.Telefono, dto.Mensaje);

            await _logs.CreateAsync(new EmailLog
            {
                Destinatario   = cliente.Telefono,
                Asunto         = dto.Mensaje.Length > 100 ? dto.Mensaje[..97] + "…" : dto.Mensaje,
                Cuerpo         = dto.Mensaje,
                Tipo           = "whatsapp",
                ReferenciaTipo = "cliente",
                ReferenciaId   = cliente.Id,
                Estado         = ok ? "enviado" : "error",
                Intentos       = 1,
                FechaEnvio     = ok ? DateTime.UtcNow : null
            });

            resultados.Add(new WhatsAppResultadoDto
            {
                ClienteId     = cliente.Id,
                ClienteNombre = $"{cliente.Nombre} {cliente.Apellidos}".Trim(),
                Telefono      = cliente.Telefono,
                Ok            = ok,
                Mensaje       = ok ? "Enviado." : "Sin Twilio — usa el enlace manual."
            });
        }

        var enviados = resultados.Count(r => r.Ok);
        return Ok(ApiResponse<List<WhatsAppResultadoDto>>.Ok(resultados,
            $"Procesados {resultados.Count} destinatarios. Enviados vía API: {enviados}."));
    }

    // GET api/whatsapp/historial
    [HttpGet("historial")]
    public async Task<ActionResult<ApiResponse<IEnumerable<WhatsAppLogDto>>>> Historial(
        [FromQuery] int limit = 100)
    {
        var logs = await _logs.GetByTipoAsync("whatsapp", limit);
        var dto  = logs.Select(l => new WhatsAppLogDto
        {
            Id             = l.Id,
            Destinatario   = l.Destinatario,
            ClienteId      = l.ReferenciaId,
            MensajeResumen = l.Asunto ?? "",
            Estado         = l.Estado,
            FechaCreacion  = l.FechaCreacion
        });
        return Ok(ApiResponse<IEnumerable<WhatsAppLogDto>>.Ok(dto));
    }
}
