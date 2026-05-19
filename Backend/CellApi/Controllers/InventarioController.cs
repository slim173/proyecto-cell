using CellApi.DTOs;
using CellApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventarioController : ControllerBase
{
    private readonly IInventarioService _service;

    public InventarioController(IInventarioService service) => _service = service;

    [HttpGet("kardex")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventarioMovimientoDto>>>> GetKardex(
        [FromQuery] int? productoId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        var data = await _service.GetKardexAsync(productoId, desde, hasta);
        return Ok(ApiResponse<IEnumerable<InventarioMovimientoDto>>.Ok(data));
    }

    [HttpGet("stock-bajo")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductoStockBajoDto>>>> GetStockBajo()
    {
        var data = await _service.GetStockBajoAsync();
        return Ok(ApiResponse<IEnumerable<ProductoStockBajoDto>>.Ok(data));
    }
}
