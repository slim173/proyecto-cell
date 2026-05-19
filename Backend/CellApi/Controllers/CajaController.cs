using CellApi.DTOs;
using CellApi.Models;
using CellApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CajaController : ControllerBase
{
    private readonly ICajaRepository _repo;

    public CajaController(ICajaRepository repo) => _repo = repo;

    private string UsuarioActual =>
        User.FindFirstValue(ClaimTypes.Name) ?? "sistema";

    [HttpGet("sesion-actual")]
    public async Task<IActionResult> GetSesionActual()
    {
        var s = await _repo.GetSesionAbiertaAsync();
        return Ok(ApiResponse<CajaSesionDto?>.Ok(s == null ? null : MapDto(s)));
    }

    [HttpGet("historial")]
    public async Task<IActionResult> GetHistorial([FromQuery] int limit = 30)
    {
        var items = await _repo.GetHistorialAsync(limit);
        return Ok(ApiResponse<IEnumerable<CajaSesionDto>>.Ok(items.Select(MapDto)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _repo.GetByIdAsync(id);
        if (s == null) return NotFound(ApiResponse.Fail("Sesión no encontrada."));
        return Ok(ApiResponse<CajaSesionDto>.Ok(MapDto(s)));
    }

    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirCajaDto dto)
    {
        var existente = await _repo.GetSesionAbiertaAsync();
        if (existente != null)
            return BadRequest(ApiResponse.Fail("Ya hay una sesión de caja abierta."));

        var sesion = new CajaSesion
        {
            EfectivoApertura = dto.EfectivoApertura,
            UsuarioApertura  = UsuarioActual,
            Observaciones    = dto.Observaciones,
            FechaApertura    = DateTime.UtcNow
        };
        var id = await _repo.AbrirSesionAsync(sesion);
        sesion.Id = id;
        return Ok(ApiResponse<CajaSesionDto>.Ok(MapDto(sesion), "Caja abierta correctamente."));
    }

    [HttpPost("{id:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int id, [FromBody] CerrarCajaDto dto)
    {
        var sesion = await _repo.GetByIdAsync(id);
        if (sesion == null) return NotFound(ApiResponse.Fail("Sesión no encontrada."));
        if (sesion.Estado != "abierta")
            return BadRequest(ApiResponse.Fail("La sesión ya está cerrada."));

        var diferencia = dto.EfectivoCierre - sesion.EfectivoApertura - sesion.TotalEfectivo;
        await _repo.CerrarSesionAsync(id, dto.EfectivoCierre, diferencia,
            dto.Observaciones, UsuarioActual);
        return Ok(ApiResponse.Ok("Caja cerrada correctamente."));
    }

    [HttpPost("{id:int}/movimientos")]
    public async Task<IActionResult> AddMovimiento(int id, [FromBody] AddMovimientoCajaDto dto)
    {
        var sesion = await _repo.GetByIdAsync(id);
        if (sesion == null) return NotFound(ApiResponse.Fail("Sesión no encontrada."));
        if (sesion.Estado != "abierta")
            return BadRequest(ApiResponse.Fail("La sesión está cerrada."));

        var mov = new CajaMovimiento
        {
            SesionId   = id,
            Tipo       = dto.Tipo,
            Concepto   = dto.Concepto,
            Importe    = dto.Importe,
            MetodoPago = dto.MetodoPago,
            Usuario    = UsuarioActual,
            Fecha      = DateTime.UtcNow
        };
        await _repo.AddMovimientoAsync(mov);
        return Ok(ApiResponse.Ok("Movimiento registrado."));
    }

    private static CajaSesionDto MapDto(CajaSesion s) => new()
    {
        Id               = s.Id,
        NumeroSesion     = s.NumeroSesion,
        FechaApertura    = s.FechaApertura,
        FechaCierre      = s.FechaCierre,
        EfectivoApertura = s.EfectivoApertura,
        EfectivoCierre   = s.EfectivoCierre,
        TotalEfectivo    = s.TotalEfectivo,
        TotalTarjeta     = s.TotalTarjeta,
        TotalOtros       = s.TotalOtros,
        Diferencia       = s.Diferencia,
        Estado           = s.Estado,
        UsuarioApertura  = s.UsuarioApertura,
        UsuarioCierre    = s.UsuarioCierre,
        Observaciones    = s.Observaciones,
        FechaCreacion    = s.FechaCreacion,
        Movimientos      = s.Movimientos.Select(m => new CajaMovimientoDto {
            Id = m.Id, SesionId = m.SesionId, Tipo = m.Tipo, Concepto = m.Concepto,
            Importe = m.Importe, MetodoPago = m.MetodoPago, ReferenciaTipo = m.ReferenciaTipo,
            ReferenciaId = m.ReferenciaId, Usuario = m.Usuario, Fecha = m.Fecha
        }).ToList()
    };
}
