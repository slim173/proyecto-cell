using CellApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/portal")]
public class PortalController : ControllerBase
{
    private readonly IReparacionRepository    _repRepo;
    private readonly IFacturaRepository       _factRepo;
    private readonly IConfiguracionRepository _config;

    public PortalController(
        IReparacionRepository    repRepo,
        IFacturaRepository       factRepo,
        IConfiguracionRepository config)
    {
        _repRepo  = repRepo;
        _factRepo = factRepo;
        _config   = config;
    }

    [HttpGet("reparacion/{id:int}")]
    public async Task<IActionResult> GetReparacion(int id)
    {
        var rep = await _repRepo.GetByIdAsync(id);
        if (rep == null) return NotFound(new { error = "Reparación no encontrada." });

        var cfg          = await _config.GetAllAsync();
        var empresaNombre = cfg.GetValueOrDefault("empresa_nombre",   "Doctor Móvil");
        var empresaTel    = cfg.GetValueOrDefault("empresa_telefono", "");
        var empresaLogo   = cfg.GetValueOrDefault("empresa_logo",     "");

        // Solo datos públicos — sin email, NIF, dirección del cliente
        var dto = new
        {
            numeroOrden          = rep.NumeroOrden,
            estado               = rep.Estado,
            estadoLabel          = EstadoLabel(rep.Estado),
            estadoColor          = EstadoColor(rep.Estado),
            estadoPaso           = EstadoPaso(rep.Estado),
            dispositivo          = rep.Dispositivo,
            marca                = rep.Marca,
            modelo               = rep.Modelo,
            descripcionFalla     = rep.DescripcionFalla,
            solucion             = EsEstadoFinal(rep.Estado) ? rep.Solucion : null,
            observaciones        = EsEstadoFinal(rep.Estado) ? rep.ObservacionesTecnico : null,
            fechaRecepcion       = rep.FechaRecepcion,
            fechaEstimadaEntrega = rep.FechaEstimadaEntrega,
            fechaEntregaReal     = rep.FechaEntregaReal,
            precioEstimado       = rep.Estado is "diagnosticado" or "en_reparacion" or "reparado" or "entregado"
                                    ? rep.PrecioEstimado : null,
            total                = rep.Estado is "reparado" or "entregado"
                                    ? rep.Total : null,
            empresaNombre,
            empresaTelefono = empresaTel,
            empresaLogo     = string.IsNullOrEmpty(empresaLogo) ? null : $"/{empresaLogo}",
        };

        return Ok(dto);
    }

    [HttpGet("factura/{id:int}")]
    public async Task<IActionResult> GetFactura(int id)
    {
        var factura = await _factRepo.GetByIdAsync(id);
        if (factura == null || factura.Anulada)
            return NotFound(new { error = "Factura no encontrada." });

        var cfg           = await _config.GetAllAsync();
        var empresaNombre = cfg.GetValueOrDefault("empresa_nombre",   "Doctor Móvil");
        var empresaTel    = cfg.GetValueOrDefault("empresa_telefono", "");

        var dto = new
        {
            numeroFactura   = factura.NumeroFactura,
            fechaEmision    = factura.FechaEmision,
            clienteNombre   = $"{factura.ClienteNombre} {factura.ClienteApellidos}".Trim(),
            baseImponible   = factura.BaseImponible,
            porcentajeIva   = factura.PorcentajeIva,
            importeIva      = factura.ImporteIva,
            total           = factura.Total,
            anulada         = factura.Anulada,
            empresaNombre,
            empresaTelefono = empresaTel,
        };

        return Ok(dto);
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private static string EstadoLabel(string e) => e switch
    {
        "recibido"      => "Recibido",
        "diagnosticado" => "Diagnosticado",
        "en_reparacion" => "En reparación",
        "reparado"      => "Listo para recoger",
        "entregado"     => "Entregado",
        "no_reparable"  => "No reparable",
        _               => e
    };

    private static string EstadoColor(string e) => e switch
    {
        "recibido"      => "secondary",
        "diagnosticado" => "info",
        "en_reparacion" => "warning",
        "reparado"      => "success",
        "entregado"     => "primary",
        "no_reparable"  => "danger",
        _               => "secondary"
    };

    private static int EstadoPaso(string e) => e switch
    {
        "recibido"      => 1,
        "diagnosticado" => 2,
        "en_reparacion" => 3,
        "reparado"      => 4,
        "entregado"     => 5,
        "no_reparable"  => 5,
        _               => 1
    };

    private static bool EsEstadoFinal(string e) =>
        e is "reparado" or "entregado" or "no_reparable";
}
