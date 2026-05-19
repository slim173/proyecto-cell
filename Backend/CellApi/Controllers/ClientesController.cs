using CellApi.DTOs;
using CellApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _service;

    public ClientesController(IClienteService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ClienteDto>>>> GetAll(
        [FromQuery] bool soloActivos = true)
    {
        var data = await _service.GetAllAsync(soloActivos);
        return Ok(ApiResponse<IEnumerable<ClienteDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound(ApiResponse<ClienteDto>.Fail($"Cliente {id} no encontrado."));
        return Ok(ApiResponse<ClienteDto>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> Create([FromBody] CreateClienteDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ClienteDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = data.Id },
                ApiResponse<ClienteDto>.Ok(data, "Cliente creado correctamente."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<ClienteDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> Update(int id, [FromBody] UpdateClienteDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<ClienteDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        try
        {
            var data = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<ClienteDto>.Ok(data, "Cliente actualizado correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ClienteDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<ClienteDto>.Fail(ex.Message)); }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Cliente desactivado correctamente."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }
}
