using CellApi.DTOs;
using CellApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _service;
    private readonly IPdfService      _pdf;

    public ProductosController(IProductoService service, IPdfService pdf)
    {
        _service = service;
        _pdf     = pdf;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductoDto>>>> GetAll(
        [FromQuery] bool soloActivos = true)
    {
        var data = await _service.GetAllAsync(soloActivos);
        return Ok(ApiResponse<IEnumerable<ProductoDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductoDto>>> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null) return NotFound(ApiResponse<ProductoDto>.Fail($"Producto {id} no encontrado."));
        return Ok(ApiResponse<ProductoDto>.Ok(data));
    }

    [HttpGet("categorias")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoriaDto>>>> GetCategorias()
    {
        var data = await _service.GetCategoriasAsync();
        return Ok(ApiResponse<IEnumerable<CategoriaDto>>.Ok(data));
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<ApiResponse<ProductoDto>>> Buscar([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(ApiResponse<ProductoDto>.Fail("Introduce un código o nombre."));
        var data = await _service.GetByCodigoAsync(q.Trim());
        if (data == null)
            return NotFound(ApiResponse<ProductoDto>.Fail("Producto no encontrado."));
        return Ok(ApiResponse<ProductoDto>.Ok(data));
    }

    [HttpGet("stock-bajo")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductoDto>>>> GetStockBajo()
    {
        var data = await _service.GetStockBajoAsync();
        return Ok(ApiResponse<IEnumerable<ProductoDto>>.Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<ProductoDto>>> Create([FromBody] CreateProductoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ProductoDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        var data = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = data.Id },
            ApiResponse<ProductoDto>.Ok(data, "Producto creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<ProductoDto>>> Update(int id, [FromBody] UpdateProductoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ProductoDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<ProductoDto>.Ok(data, "Producto actualizado correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ProductoDto>.Fail(ex.Message)); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Producto desactivado correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("ajuste-stock")]
    public async Task<ActionResult<ApiResponse>> AjustarStock([FromBody] AjusteStockDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).First()));
        try
        {
            await _service.AjustarStockAsync(dto);
            return Ok(ApiResponse.Ok("Stock ajustado correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("etiquetas-precio")]
    public async Task<IActionResult> EtiquetasPrecio(
        [FromQuery] string ids, [FromQuery] string formato = "50x30")
    {
        var idList = ids.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
            .Where(x => x.HasValue).Select(x => x!.Value).ToList();
        if (!idList.Any())
            return BadRequest(ApiResponse.Fail("Selecciona al menos un producto."));

        var todos = await _service.GetAllAsync(soloActivos: false);
        var seleccionados = todos.Where(p => idList.Contains(p.Id));
        var bytes = await _pdf.GenerarEtiquetasPrecioPdfAsync(seleccionados, formato);
        Response.Headers.Append("Content-Disposition", "inline; filename=\"etiquetas.pdf\"");
        return File(bytes, "application/pdf");
    }
}
