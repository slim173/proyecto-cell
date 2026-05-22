using CellApi.DTOs;
using CellApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FacturasController : ControllerBase
{
    private readonly IFacturaService _service;
    private readonly IEmailService   _email;
    private readonly IPdfService     _pdf;

    public FacturasController(IFacturaService service, IEmailService email, IPdfService pdf)
    {
        _service = service;
        _email   = email;
        _pdf     = pdf;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CrearFacturaResponseDto>>> Create([FromBody] CreateFacturaDto dto)
    {
        if (dto.ClienteId <= 0)
            return BadRequest(ApiResponse<CrearFacturaResponseDto>.Fail("Debe seleccionar un cliente."));
        if (dto.Lineas.Count == 0)
            return BadRequest(ApiResponse<CrearFacturaResponseDto>.Fail("La factura debe tener al menos una línea."));
        if (dto.Lineas.Any(l => string.IsNullOrWhiteSpace(l.Descripcion)))
            return BadRequest(ApiResponse<CrearFacturaResponseDto>.Fail("Todas las líneas deben tener descripción."));
        if (dto.Lineas.Any(l => l.Cantidad <= 0 || l.PrecioUnitario <= 0))
            return BadRequest(ApiResponse<CrearFacturaResponseDto>.Fail("Cantidad y precio unitario deben ser mayores que 0."));

        var result = await _service.CreateManualAsync(dto);
        return Ok(ApiResponse<CrearFacturaResponseDto>.Ok(result,
            $"Factura {result.NumeroFactura} creada correctamente."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<FacturaDto>>>> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<FacturaDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<FacturaDto>>> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null) return NotFound(ApiResponse<FacturaDto>.Fail($"Factura {id} no encontrada."));
        return Ok(ApiResponse<FacturaDto>.Ok(data));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DescargarPdf(int id, [FromQuery] string? formato = null)
    {
        try
        {
            var factura = await _service.GetByIdAsync(id);
            if (factura == null) return NotFound(ApiResponse.Fail($"Factura {id} no encontrada."));

            var fmtValido = formato is "a4" or "ticket_80mm" or "ticket_58mm" ? formato : null;
            var bytes  = await _service.DescargarPdfAsync(id, fmtValido);
            var nombre = $"Factura_{factura.NumeroFactura.Replace("/", "-")}.pdf";
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{nombre}\"");
            return File(bytes, "application/pdf");
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (Exception ex)
        {
            var msg = $"ERROR PDF factura {id}: {ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null) msg += $"\nCausa: {ex.InnerException.Message}";
            return StatusCode(500, msg);
        }
    }

    [HttpPost("{id:int}/enviar-email")]
    public async Task<ActionResult<ApiResponse>> EnviarEmail(int id, [FromBody] EnviarPdfDto dto)
    {
        var factura = await _service.GetByIdAsync(id);
        if (factura == null) return NotFound(ApiResponse.Fail($"Factura {id} no encontrada."));
        if (factura.Anulada) return BadRequest(ApiResponse.Fail("No se puede enviar una factura anulada."));

        var dest = !string.IsNullOrWhiteSpace(dto.Destinatario)
            ? dto.Destinatario
            : factura.ClienteEmail;

        if (string.IsNullOrWhiteSpace(dest))
            return BadRequest(ApiResponse.Fail("El cliente no tiene email registrado."));

        byte[] pdfBytes;
        try { pdfBytes = await _service.DescargarPdfAsync(id); }
        catch { return BadRequest(ApiResponse.Fail("El PDF de esta factura no está disponible todavía.")); }

        var nombre = $"Factura_{factura.NumeroFactura.Replace("/", "-")}.pdf";
        var asunto = $"Factura {factura.NumeroFactura}";
        var cuerpo = $@"
            <p>Estimado/a {factura.ClienteNombreCompleto},</p>
            <p>Adjunto encontrará la factura <strong>{factura.NumeroFactura}</strong>
               de fecha {factura.FechaEmision:dd/MM/yyyy}.</p>
            <p><strong>Total:</strong> {factura.Total:F2} € (IVA {factura.PorcentajeIva:0}% incluido)</p>
            <p>Quedo a su disposición para cualquier consulta.</p>";

        await _email.SendAsync(dest, asunto, cuerpo, "factura", "factura", id, pdfBytes, nombre);
        return Ok(ApiResponse.Ok($"Factura enviada a {dest}"));
    }

    [HttpPost("{id:int}/anular")]
    public async Task<ActionResult<ApiResponse>> Anular(int id, [FromBody] AnularFacturaDto dto)
    {
        try
        {
            await _service.AnularAsync(id, dto.MotivoAnulacion);
            return Ok(ApiResponse.Ok("Factura anulada correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }
}
