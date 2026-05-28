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

    // ── Página HTML pública (el QR apunta aquí) ─────────────────────
    [HttpGet("/portal/reparacion/{id:int}")]
    public IActionResult HtmlReparacion(int id)
    {
        var html = $$"""
<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>Estado de reparación</title>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:#f4f6f9;min-height:100vh}
.wrap{max-width:480px;margin:0 auto;background:#fff;min-height:100vh}
.hdr{background:#1a1a2e;color:#fff;text-align:center;padding:18px 16px}
.hdr img{max-height:48px;max-width:140px;object-fit:contain;display:block;margin:0 auto 6px}
.hdr .biz{font-size:1.1rem;font-weight:700}
.hdr a{color:rgba(255,255,255,.6);font-size:.82rem;text-decoration:none}
.orden{text-align:center;padding:16px;border-bottom:1px solid #e9ecef}
.orden .lbl{font-size:.72rem;text-transform:uppercase;font-weight:700;color:#6c757d;letter-spacing:.05em}
.orden .num{font-size:2rem;font-weight:800;color:#0d6efd}
.orden .fecha{font-size:.8rem;color:#6c757d}
.estado-wrap{text-align:center;padding:14px}
.badge{display:inline-block;padding:8px 22px;border-radius:50px;font-size:1rem;font-weight:700;color:#fff}
.steps{display:flex;align-items:center;padding:6px 12px 16px;justify-content:center}
.step{display:flex;flex-direction:column;align-items:center;gap:4px}
.sc{width:28px;height:28px;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:.75rem;font-weight:700}
.done .sc{background:#198754;color:#fff}.active .sc{background:#0d6efd;color:#fff}.pend .sc{background:#dee2e6;color:#6c757d}
.sl{font-size:.6rem;text-align:center;max-width:50px;color:#6c757d;line-height:1.2}
.done .sl{color:#198754}.active .sl{color:#0d6efd;font-weight:700}
.line{flex:1;height:2px;background:#dee2e6;margin:0 2px;margin-bottom:16px}
.line.done{background:#198754}
.card{background:#f8f9fa;border:1px solid #e9ecef;border-radius:12px;padding:13px 15px;margin:0 14px 12px}
.card-title{font-size:.7rem;font-weight:700;text-transform:uppercase;color:#6c757d;letter-spacing:.05em;margin-bottom:5px}
.cta{padding:0 14px 28px;margin-top:8px}
.cta a{display:block;background:#198754;color:#fff;text-align:center;padding:14px;border-radius:10px;text-decoration:none;font-size:1rem;font-weight:700}
.spin{display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:60vh;color:#6c757d}
.spinner{width:40px;height:40px;border:4px solid #dee2e6;border-top-color:#0d6efd;border-radius:50%;animation:spin .8s linear infinite}
@keyframes spin{to{transform:rotate(360deg)}}
.err{text-align:center;padding:3rem 1.5rem;color:#dc3545}
</style>
</head>
<body>
<div class="wrap" id="app">
  <div class="spin"><div class="spinner"></div><p style="margin-top:12px">Cargando...</p></div>
</div>
<script>
const ID = {{id}};
const ESTADOS = ['recibido','diagnosticado','en_reparacion','reparado','entregado'];
const LABELS  = {recibido:'Recibido',diagnosticado:'Diagnosticado',en_reparacion:'En reparación',reparado:'Listo para recoger',entregado:'Entregado',no_reparable:'No reparable'};
const STEP_L  = ['Recibido','Diagnóstico','Reparando','Listo','Entregado'];
const COLORS  = {recibido:'#6c757d',diagnosticado:'#0dcaf0',en_reparacion:'#ffc107',reparado:'#198754',entregado:'#0d6efd',no_reparable:'#dc3545'};

fetch(`/api/portal/reparacion/${ID}`)
  .then(r=>r.ok?r.json():Promise.reject(r.status))
  .then(render)
  .catch(()=>{ document.getElementById('app').innerHTML='<div class="err"><p style="font-size:2rem">⚠️</p><p>No se encontró la reparación.</p></div>'; });

function render(d){
  const paso = d.estadoPaso;
  const color = COLORS[d.estado]||'#6c757d';

  // Cabecera
  let hdr = `<div class="hdr">`;
  if(d.empresaLogo) hdr += `<img src="${d.empresaLogo}" alt="logo"/>`;
  hdr += `<div class="biz">${d.empresaNombre}</div>`;
  if(d.empresaTelefono) hdr += `<a href="tel:${d.empresaTelefono}">📞 ${d.empresaTelefono}</a>`;
  hdr += `</div>`;

  // Orden
  const fechaRec = new Date(d.fechaRecepcion).toLocaleDateString('es-ES');
  const orden = `<div class="orden"><div class="lbl">Orden de reparación</div><div class="num">${d.numeroOrden}</div><div class="fecha">${fechaRec}</div></div>`;

  // Badge estado
  const badge = `<div class="estado-wrap"><span class="badge" style="background:${color}">${d.estadoLabel}</span></div>`;

  // Steps (no para no_reparable)
  let steps = '';
  if(d.estado !== 'no_reparable'){
    steps = '<div class="steps">';
    for(let i=1;i<=5;i++){
      const cls = i<paso?'step done':i===paso?'step active':'step pend';
      const icon = i<paso?'✓':i;
      steps += `<div class="${cls}"><div class="sc">${icon}</div><div class="sl">${STEP_L[i-1]}</div></div>`;
      if(i<5) steps += `<div class="line${i<paso?' done':''}"></div>`;
    }
    steps += '</div>';
  }

  // Dispositivo
  let cards = `<div class="card"><div class="card-title">📱 Dispositivo</div><div style="font-weight:600">${d.dispositivo} ${d.marca} ${d.modelo}</div><div style="font-size:.85rem;color:#6c757d;margin-top:4px">${d.descripcionFalla}</div></div>`;

  // Trabajo realizado
  if(d.solucion) cards += `<div class="card"><div class="card-title">🔧 Trabajo realizado</div><div style="font-size:.9rem">${d.solucion}</div></div>`;
  else if(d.observaciones) cards += `<div class="card"><div class="card-title">💬 Observaciones</div><div style="font-size:.9rem">${d.observaciones}</div></div>`;

  // Precio
  if(d.total!=null) cards += `<div class="card"><div class="card-title">💶 Total a pagar (IVA incluido)</div><div style="font-size:1.6rem;font-weight:800;color:#198754">${d.total.toFixed(2)} €</div></div>`;
  else if(d.precioEstimado!=null) cards += `<div class="card"><div class="card-title">💶 Presupuesto estimado (IVA incluido)</div><div style="font-size:1.3rem;font-weight:700;color:#0d6efd">${d.precioEstimado.toFixed(2)} €</div></div>`;

  // Fechas
  if(d.fechaEntregaReal) cards += `<div class="card"><div class="card-title">📅 Entregado el</div><div style="font-weight:600">${new Date(d.fechaEntregaReal).toLocaleDateString('es-ES')}</div></div>`;
  else if(d.fechaEstimadaEntrega) cards += `<div class="card"><div class="card-title">📅 Fecha estimada de entrega</div><div style="font-weight:600">${new Date(d.fechaEstimadaEntrega).toLocaleDateString('es-ES')}</div></div>`;

  // CTA
  let cta = '';
  if(d.empresaTelefono) cta = `<div class="cta"><a href="tel:${d.empresaTelefono}">📞 Llamar a ${d.empresaNombre}</a></div>`;

  const ts = new Date().toLocaleString('es-ES');
  document.getElementById('app').innerHTML = hdr+orden+badge+steps+cards+cta+`<div style="text-align:center;padding-bottom:20px;font-size:.75rem;color:#adb5bd">Actualizado: ${ts}</div>`;
}
</script>
</body>
</html>
""";
        return Content(html, "text/html; charset=utf-8");
    }

    // ── JSON API (llamada interna del portal Blazor) ──────────────────
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
