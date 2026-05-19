using CellApi.DTOs;
using CellApi.Models;
using CellApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GarantiasController : ControllerBase
{
    private readonly IGarantiaRepository _repo;
    private readonly IConfiguracionRepository _config;

    public GarantiasController(IGarantiaRepository repo, IConfiguracionRepository config)
    {
        _repo   = repo;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? clienteId = null)
    {
        var items = await _repo.GetAllAsync(clienteId);
        var dtos  = items.Select(MapDto);
        return Ok(ApiResponse<IEnumerable<GarantiaDto>>.Ok(dtos));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var g = await _repo.GetByIdAsync(id);
        if (g == null) return NotFound(ApiResponse.Fail("Garantía no encontrada."));
        return Ok(ApiResponse<GarantiaDto>.Ok(MapDto(g)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGarantiaDto dto)
    {
        var cfg   = await _config.GetAllAsync();
        if (!int.TryParse(cfg.GetValueOrDefault("garantia_meses_defecto", "12"), out var mesesDef))
            mesesDef = 12;

        var meses = dto.Meses > 0 ? dto.Meses : mesesDef;
        var inicio = dto.FechaInicio == default ? DateTime.UtcNow : dto.FechaInicio;

        var g = new Garantia
        {
            NumeroGarantia      = null,
            Tipo                = dto.Tipo,
            ReferenciaId        = dto.ReferenciaId,
            ClienteId           = dto.ClienteId,
            ProductoDescripcion = dto.ProductoDescripcion,
            FechaInicio         = inicio,
            FechaFin            = inicio.AddMonths(meses),
            Meses               = meses,
            Estado              = "activa",
            Observaciones       = dto.Observaciones,
            FechaCreacion       = DateTime.UtcNow
        };

        // Generar número
        g.NumeroGarantia = await GenerarNumero();
        var id = await _repo.CreateAsync(g);
        g.Id = id;
        return Ok(ApiResponse<GarantiaDto>.Ok(MapDto(g), "Garantía creada correctamente."));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateGarantiaEstadoDto dto)
    {
        var g = await _repo.GetByIdAsync(id);
        if (g == null) return NotFound(ApiResponse.Fail("Garantía no encontrada."));
        await _repo.UpdateEstadoAsync(id, dto.Estado);
        return Ok(ApiResponse.Ok("Estado actualizado."));
    }

    private async Task<string> GenerarNumero()
    {
        // Formato G-YYYY####
        var anio = DateTime.UtcNow.Year;
        var todos = await _repo.GetAllAsync();
        var contador = todos.Count(g => g.FechaCreacion.Year == anio) + 1;
        return $"G-{anio}{contador:D4}";
    }

    private static GarantiaDto MapDto(Garantia g) => new()
    {
        Id                    = g.Id,
        NumeroGarantia        = g.NumeroGarantia,
        Tipo                  = g.Tipo,
        ReferenciaId          = g.ReferenciaId,
        ClienteId             = g.ClienteId,
        ClienteNombreCompleto = $"{g.ClienteNombre} {g.ClienteApellidos}".Trim(),
        ClienteTelefono       = g.ClienteTelefono,
        ProductoDescripcion   = g.ProductoDescripcion,
        FechaInicio           = g.FechaInicio,
        FechaFin              = g.FechaFin,
        Meses                 = g.Meses,
        Estado                = g.Estado,
        Observaciones         = g.Observaciones,
        FechaCreacion         = g.FechaCreacion
    };
}
