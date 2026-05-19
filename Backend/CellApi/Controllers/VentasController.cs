using CellApi.DTOs;
using CellApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly IVentaService _service;
    private readonly IPdfService   _pdf;
    private readonly IEmailService _email;

    public VentasController(IVentaService service, IPdfService pdf, IEmailService email)
    {
        _service = service;
        _pdf     = pdf;
        _email   = email;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<VentaDto>>>> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<VentaDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<VentaDto>>> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null) return NotFound(ApiResponse<VentaDto>.Fail($"Venta {id} no encontrada."));
        return Ok(ApiResponse<VentaDto>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VentaDto>>> Create([FromBody] CreateVentaDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<VentaDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = data.Id },
                ApiResponse<VentaDto>.Ok(data, "Venta creada y factura generada correctamente."));
        }
        catch (KeyNotFoundException ex)      { return NotFound(ApiResponse<VentaDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<VentaDto>.Fail(ex.Message)); }
        catch (Exception ex)                 { return StatusCode(500, ApiResponse<VentaDto>.Fail("Error interno: " + ex.Message)); }
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<ApiResponse>> UpdateEstado(int id, [FromBody] UpdateVentaEstadoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).First()));
        try
        {
            await _service.UpdateEstadoAsync(id, dto.Estado);
            return Ok(ApiResponse.Ok("Estado actualizado correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (ArgumentException ex)    { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("{id:int}/generar-factura")]
    public async Task<ActionResult<ApiResponse>> GenerarFactura(int id)
    {
        try
        {
            await _service.GenerarFacturaPendienteAsync(id);
            return Ok(ApiResponse.Ok("Factura generada correctamente."));
        }
        catch (KeyNotFoundException ex)      { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (Exception ex)                 { return StatusCode(500, ApiResponse.Fail("Error al generar factura: " + ex.Message)); }
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DescargarPdf(int id, [FromQuery] string? formato = null)
    {
        var venta = await _service.GetByIdAsync(id);
        if (venta == null) return NotFound();
        var fmtValido = formato is "a4" or "ticket_80mm" or "ticket_58mm" ? formato : null;
        var bytes  = await _pdf.GenerarTicketVentaPdfAsync(venta, fmtValido);
        var nombre = $"Venta_{venta.NumeroVenta.Replace("/", "-")}.pdf";
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{nombre}\"");
        return File(bytes, "application/pdf");
    }

    [HttpPost("{id:int}/enviar-pdf")]
    public async Task<ActionResult<ApiResponse>> EnviarPdf(int id, [FromBody] EnviarPdfDto dto)
    {
        var venta = await _service.GetByIdAsync(id);
        if (venta == null) return NotFound(ApiResponse.Fail($"Venta {id} no encontrada."));

        var dest = !string.IsNullOrWhiteSpace(dto.Destinatario)
            ? dto.Destinatario
            : venta.ClienteEmail;

        if (string.IsNullOrWhiteSpace(dest))
            return BadRequest(ApiResponse.Fail("El cliente no tiene email registrado."));

        var pdfBytes = await _pdf.GenerarTicketVentaPdfAsync(venta);
        var nombre   = $"Ticket_{venta.NumeroVenta.Replace("/", "-")}.pdf";
        var asunto   = $"Ticket de venta {venta.NumeroVenta}";
        var cuerpo   = $@"
            <p>Estimado/a {venta.ClienteNombreCompleto},</p>
            <p>Adjunto encontrará el ticket de la venta <strong>{venta.NumeroVenta}</strong>
               del {venta.Fecha:dd/MM/yyyy}.</p>
            <p><strong>Total:</strong> {venta.Total:F2} € (IVA incluido)</p>
            <p>Gracias por su compra.</p>";

        await _email.SendAsync(dest, asunto, cuerpo, "ticket_venta", "venta", id, pdfBytes, nombre);
        return Ok(ApiResponse.Ok($"Ticket enviado a {dest}"));
    }

    [HttpPost("{id:int}/enviar-factura")]
    public async Task<ActionResult<ApiResponse>> EnviarFactura(int id)
    {
        try
        {
            await _service.EnviarFacturaAsync(id);
            return Ok(ApiResponse.Ok("Factura enviada correctamente por email."));
        }
        catch (KeyNotFoundException ex)   { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }
}
