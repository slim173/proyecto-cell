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
public class ReparacionesController : ControllerBase
{
    private readonly IReparacionService    _service;
    private readonly IReparacionRepository _repo;
    private readonly IWebHostEnvironment   _env;
    private readonly IPdfService           _pdf;
    private readonly IEmailService         _email;
    private readonly IFacturaRepository    _facturas;

    public ReparacionesController(
        IReparacionService    service,
        IReparacionRepository repo,
        IWebHostEnvironment   env,
        IPdfService           pdf,
        IEmailService         email,
        IFacturaRepository    facturas)
    {
        _service  = service;
        _repo     = repo;
        _env      = env;
        _pdf      = pdf;
        _email    = email;
        _facturas = facturas;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReparacionDto>>>> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<ReparacionDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ReparacionDto>>> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null) return NotFound(ApiResponse<ReparacionDto>.Fail($"Reparación {id} no encontrada."));
        return Ok(ApiResponse<ReparacionDto>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReparacionDto>>> Create([FromBody] CreateReparacionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ReparacionDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = data.Id },
                ApiResponse<ReparacionDto>.Ok(data, "Orden de reparación creada."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ReparacionDto>.Fail(ex.Message)); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ReparacionDto>>> Update(
        int id, [FromBody] UpdateReparacionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ReparacionDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<ReparacionDto>.Ok(data, "Orden de reparación actualizada."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ReparacionDto>.Fail(ex.Message)); }
        catch (ArgumentException ex)    { return BadRequest(ApiResponse<ReparacionDto>.Fail(ex.Message)); }
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<ApiResponse<ReparacionDto>>> UpdateEstado(
        int id, [FromBody] UpdateReparacionEstadoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ReparacionDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.UpdateEstadoAsync(id, dto);
            return Ok(ApiResponse<ReparacionDto>.Ok(data, "Estado actualizado correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ReparacionDto>.Fail(ex.Message)); }
        catch (ArgumentException ex)    { return BadRequest(ApiResponse<ReparacionDto>.Fail(ex.Message)); }
    }

    [HttpPost("{id:int}/detalles")]
    public async Task<ActionResult<ApiResponse<ReparacionDetalleDto>>> AddDetalle(
        int id, [FromBody] AddReparacionDetalleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ReparacionDetalleDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.AddDetalleAsync(id, dto);
            return Ok(ApiResponse<ReparacionDetalleDto>.Ok(data, "Detalle añadido correctamente."));
        }
        catch (KeyNotFoundException ex)      { return NotFound(ApiResponse<ReparacionDetalleDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<ReparacionDetalleDto>.Fail(ex.Message)); }
    }

    [HttpDelete("{id:int}/detalles/{detalleId:int}")]
    public async Task<ActionResult<ApiResponse>> RemoveDetalle(int id, int detalleId)
    {
        try
        {
            await _service.RemoveDetalleAsync(id, detalleId);
            return Ok(ApiResponse.Ok("Detalle eliminado correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("{id:int}/notificar")]
    public async Task<ActionResult<ApiResponse>> Notificar(int id)
    {
        try
        {
            await _service.EnviarNotificacionAsync(id);
            return Ok(ApiResponse.Ok("Notificación enviada correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    // ── PDF ──────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DescargarPdf(int id, [FromQuery] string? formato = null)
    {
        try
        {
            var rep = await _service.GetByIdAsync(id);
            if (rep == null) return NotFound();
            var fmtValido = formato is "a4" or "ticket_80mm" or "ticket_58mm" ? formato : null;
            var bytes  = await _pdf.GenerarOrdenReparacionPdfAsync(rep, fmtValido);
            var nombre = $"Orden_{rep.NumeroOrden.Replace("/", "-")}.pdf";
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{nombre}\"");
            return File(bytes, "application/pdf");
        }
        catch (Exception ex)
        {
            var msg = $"ERROR PDF reparacion {id}: {ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null) msg += $"\nCausa: {ex.InnerException.Message}";
            return StatusCode(500, msg);
        }
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/etiqueta")]
    public async Task<IActionResult> DescargarEtiqueta(int id)
    {
        try
        {
            var rep = await _service.GetByIdAsync(id);
            if (rep == null) return NotFound();
            var bytes  = await _pdf.GenerarEtiquetaReparacionPdfAsync(rep);
            var nombre = $"Etiqueta_{rep.NumeroOrden.Replace("/", "-")}.pdf";
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{nombre}\"");
            return File(bytes, "application/pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"ERROR etiqueta {id}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [HttpPost("{id:int}/enviar-pdf")]
    public async Task<ActionResult<ApiResponse>> EnviarPdf(int id, [FromBody] EnviarPdfDto dto)
    {
        var rep = await _service.GetByIdAsync(id);
        if (rep == null) return NotFound(ApiResponse.Fail($"Reparación {id} no encontrada."));

        var dest = !string.IsNullOrWhiteSpace(dto.Destinatario)
            ? dto.Destinatario
            : rep.ClienteEmail;

        if (string.IsNullOrWhiteSpace(dest))
            return BadRequest(ApiResponse.Fail("El cliente no tiene email registrado."));

        var pdfBytes = await _pdf.GenerarOrdenReparacionPdfAsync(rep);
        var nombre   = $"Orden_{rep.NumeroOrden.Replace("/", "-")}.pdf";
        var asunto   = $"Orden de reparación {rep.NumeroOrden}";
        var lineaPrecio = rep.Total.HasValue
            ? $"<p><strong>Total a pagar:</strong> {rep.Total:F2} € (IVA incluido)</p>"
            : rep.PrecioEstimado.HasValue
                ? $"<p><strong>Presupuesto estimado:</strong> {rep.PrecioEstimado:F2} €</p>"
                : "";

        var cuerpo = $@"
            <p>Estimado/a {rep.ClienteNombreCompleto},</p>
            <p>Adjunto encontrará la orden de reparación <strong>{rep.NumeroOrden}</strong>
               correspondiente a su <strong>{rep.Dispositivo} {rep.Marca} {rep.Modelo}</strong>.</p>
            <p><strong>Descripción de la falla:</strong> {rep.DescripcionFalla}</p>
            {lineaPrecio}
            <p>Cualquier consulta, no dude en contactarnos.</p>
            <p>Un saludo.</p>";

        await _email.SendAsync(dest, asunto, cuerpo, "orden_reparacion", "reparacion", id, pdfBytes, nombre);
        return Ok(ApiResponse.Ok($"Orden de reparación enviada a {dest}"));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/factura-id")]
    public async Task<ActionResult<ApiResponse<int?>>> GetFacturaId(int id)
    {
        var factura = await _facturas.GetByReparacionIdAsync(id);
        return Ok(ApiResponse<int?>.Ok(factura?.Id));
    }

    [HttpGet("{id:int}/historial-equipo")]
    public async Task<ActionResult<ApiResponse<IEnumerable<HistorialEquipoDto>>>> GetHistorialEquipo(int id)
    {
        try
        {
            var historial = await _service.GetHistorialEquipoAsync(id);
            return Ok(ApiResponse<IEnumerable<HistorialEquipoDto>>.Ok(historial));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<IEnumerable<HistorialEquipoDto>>.Fail(ex.Message)); }
    }

    // ── Imágenes ─────────────────────────────────────────────────
    [HttpPost("{id:int}/imagenes")]
    [RequestSizeLimit(52_428_800)] // 50 MB máximo por request
    public async Task<ActionResult<ApiResponse<List<ReparacionImagenDto>>>> SubirImagenes(
        int id, [FromForm] IFormFileCollection archivos)
    {
        if (archivos == null || archivos.Count == 0)
            return BadRequest(ApiResponse<List<ReparacionImagenDto>>.Fail("No se recibieron archivos."));

        // Validar que la reparación existe
        var rep = await _service.GetByIdAsync(id);
        if (rep == null)
            return NotFound(ApiResponse<List<ReparacionImagenDto>>.Fail($"Reparación {id} no encontrada."));

        // Extensiones permitidas
        var extensionesPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

        var guardadas = new List<ReparacionImagenDto>();
        var dirRep    = Path.Combine(
            _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            "reparaciones", id.ToString());

        Directory.CreateDirectory(dirRep);

        foreach (var archivo in archivos)
        {
            var ext = Path.GetExtension(archivo.FileName).ToLower();
            if (!extensionesPermitidas.Contains(ext)) continue;
            if (archivo.Length == 0) continue;

            var nombreArchivo = $"{Guid.NewGuid()}{ext}";
            var rutaFisica    = Path.Combine(dirRep, nombreArchivo);
            var rutaPublica   = $"/reparaciones/{id}/{nombreArchivo}";

            await using var stream = System.IO.File.Create(rutaFisica);
            await archivo.CopyToAsync(stream);

            var imagen = new ReparacionImagen
            {
                ReparacionId  = id,
                RutaImagen    = rutaPublica,
                NombreArchivo = archivo.FileName
            };

            var nuevaId = await _repo.AddImagenAsync(imagen);
            guardadas.Add(new ReparacionImagenDto
            {
                Id            = nuevaId,
                ReparacionId  = id,
                RutaImagen    = rutaPublica,
                NombreArchivo = archivo.FileName,
                Fecha         = DateTime.UtcNow
            });
        }

        if (!guardadas.Any())
            return BadRequest(ApiResponse<List<ReparacionImagenDto>>.Fail(
                "Ningún archivo válido fue procesado. Use imágenes JPG, PNG, GIF o WebP."));

        return Ok(ApiResponse<List<ReparacionImagenDto>>.Ok(
            guardadas, $"{guardadas.Count} imagen(es) guardada(s) correctamente."));
    }

    [HttpGet("{id:int}/imagenes")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReparacionImagenDto>>>> GetImagenes(int id)
    {
        var imagenes = await _repo.GetImagenesAsync(id);
        var dtos = imagenes.Select(img => new ReparacionImagenDto
        {
            Id            = img.Id,
            ReparacionId  = img.ReparacionId,
            RutaImagen    = img.RutaImagen,
            NombreArchivo = img.NombreArchivo,
            Fecha         = img.Fecha
        });
        return Ok(ApiResponse<IEnumerable<ReparacionImagenDto>>.Ok(dtos));
    }

    [HttpDelete("{id:int}/imagenes/{imagenId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> EliminarImagen(int id, int imagenId)
    {
        // Validate the image belongs to this repair
        var imagenes = await _repo.GetImagenesAsync(id);
        var imagen   = imagenes.FirstOrDefault(i => i.Id == imagenId);
        if (imagen == null)
            return NotFound(ApiResponse<object>.Fail("Imagen no encontrada."));

        // Delete physical file
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var rutaFisica = Path.Combine(webRoot,
            imagen.RutaImagen.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(rutaFisica))
            System.IO.File.Delete(rutaFisica);

        await _repo.DeleteImagenAsync(imagenId);
        return Ok(ApiResponse<object>.Ok(new object(), "Imagen eliminada correctamente."));
    }

    // ── Seguimiento público ──────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("seguimiento")]
    public async Task<IActionResult> Seguimiento([FromQuery] string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return BadRequest(ApiResponse.Fail("Indica el número de orden."));
        var rep = await _service.GetByNumeroOrdenAsync(numero.Trim().ToUpper());
        if (rep == null) return NotFound(ApiResponse.Fail("Orden no encontrada."));

        var pasos = new[] { "recibido","diagnosticado","en_reparacion","reparado","entregado" };
        var estadoActual = rep.Estado == "no_reparable" ? "no_reparable" : rep.Estado;
        var pasoActual   = Array.IndexOf(pasos, estadoActual);

        return Ok(ApiResponse<object>.Ok(new {
            numeroOrden      = rep.NumeroOrden,
            dispositivo      = $"{rep.Dispositivo} {rep.Marca} {rep.Modelo}".Trim(),
            estado           = estadoActual,
            estadoLabel      = EstadoLabel(rep.Estado),
            prioridad        = rep.Prioridad,
            fechaRecepcion   = rep.FechaRecepcion,
            fechaEntrega     = rep.FechaEstimadaEntrega,
            fechaEntregaReal = rep.FechaEntregaReal,
            pasoActual,
            totalPasos       = pasos.Length,
            pasos            = pasos,
            presupuesto      = rep.PrecioEstimado,
            total            = rep.Total,
            solucion         = rep.Estado == "entregado" || rep.Estado == "reparado"
                                 ? rep.Solucion : null
        }));
    }

    private static string EstadoLabel(string estado) => estado switch {
        "recibido"      => "Recibido",
        "diagnosticado" => "Diagnosticado",
        "en_reparacion" => "En reparación",
        "reparado"      => "Reparado",
        "entregado"     => "Entregado",
        "no_reparable"  => "No reparable",
        _               => estado
    };
}
