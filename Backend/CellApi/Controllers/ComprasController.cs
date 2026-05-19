using CellApi.DTOs;
using CellApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ComprasController : ControllerBase
{
    private readonly ICompraService _service;
    private readonly IPdfService    _pdf;
    private readonly IEmailService  _email;

    public ComprasController(ICompraService service, IPdfService pdf, IEmailService email)
    {
        _service = service;
        _pdf     = pdf;
        _email   = email;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CompraDto>>>> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CompraDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CompraDto>>> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null) return NotFound(ApiResponse<CompraDto>.Fail($"Compra {id} no encontrada."));
        return Ok(ApiResponse<CompraDto>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CompraDto>>> Create([FromBody] CreateCompraDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<CompraDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = data.Id },
                ApiResponse<CompraDto>.Ok(data, "Compra registrada. Stock actualizado."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<CompraDto>.Fail(ex.Message)); }
    }

    // ── PDF ──────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        var compra = await _service.GetByIdAsync(id);
        if (compra == null) return NotFound();
        var bytes  = await _pdf.GenerarOrdenCompraPdfAsync(compra);
        var nombre = $"Compra_{compra.NumeroCompra.Replace("/", "-")}.pdf";
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{nombre}\"");
        return File(bytes, "application/pdf");
    }

    [HttpPost("{id:int}/enviar-pdf")]
    public async Task<ActionResult<ApiResponse>> EnviarPdf(int id, [FromBody] EnviarPdfDto dto)
    {
        var compra = await _service.GetByIdAsync(id);
        if (compra == null) return NotFound(ApiResponse.Fail($"Compra {id} no encontrada."));

        var dest = dto.Destinatario?.Trim();
        if (string.IsNullOrWhiteSpace(dest))
        {
            // Intentar obtener email del proveedor
            var proveedores = await _service.GetProveedoresAsync(soloActivos: false);
            dest = proveedores.FirstOrDefault(p => p.Id == compra.ProveedorId)?.Email;
        }

        if (string.IsNullOrWhiteSpace(dest))
            return BadRequest(ApiResponse.Fail("El proveedor no tiene email registrado. Indique un destinatario."));

        var pdfBytes = await _pdf.GenerarOrdenCompraPdfAsync(compra);
        var nombre   = $"Compra_{compra.NumeroCompra.Replace("/", "-")}.pdf";
        var asunto   = $"Orden de compra {compra.NumeroCompra}";
        var cuerpo   = $@"
            <p>Estimado proveedor {compra.ProveedorNombre},</p>
            <p>Adjunto encontrará la orden de compra <strong>{compra.NumeroCompra}</strong>
               del {compra.Fecha:dd/MM/yyyy}.</p>
            <p><strong>Total:</strong> {compra.Total:F2} €</p>
            <p>Quedo a su disposición para cualquier consulta.</p>";

        await _email.SendAsync(dest, asunto, cuerpo, "orden_compra", "compra", id, pdfBytes, nombre);
        return Ok(ApiResponse.Ok($"Orden de compra enviada a {dest}"));
    }

    // ── Proveedores ──────────────────────────────────────────────

    [HttpGet("proveedores")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProveedorDto>>>> GetProveedores(
        [FromQuery] bool soloActivos = true)
    {
        var data = await _service.GetProveedoresAsync(soloActivos);
        return Ok(ApiResponse<IEnumerable<ProveedorDto>>.Ok(data));
    }

    [HttpPost("proveedores")]
    public async Task<ActionResult<ApiResponse<ProveedorDto>>> CreateProveedor([FromBody] CreateProveedorDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ProveedorDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var data = await _service.CreateProveedorAsync(dto);
        return Ok(ApiResponse<ProveedorDto>.Ok(data, "Proveedor creado correctamente."));
    }

    [HttpPut("proveedores/{id:int}")]
    public async Task<ActionResult<ApiResponse<ProveedorDto>>> UpdateProveedor(
        int id, [FromBody] UpdateProveedorDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ProveedorDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.UpdateProveedorAsync(id, dto);
            return Ok(ApiResponse<ProveedorDto>.Ok(data, "Proveedor actualizado correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ProveedorDto>.Fail(ex.Message)); }
    }
}
