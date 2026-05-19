using AutoMapper;
using CellApi.DTOs;
using CellApi.Repositories;

namespace CellApi.Services;

public class FacturaService : IFacturaService
{
    private readonly IFacturaRepository     _repo;
    private readonly IVentaRepository       _ventaRepo;
    private readonly IReparacionRepository  _repRepo;
    private readonly IPdfService            _pdfService;
    private readonly IMapper                _mapper;

    public FacturaService(
        IFacturaRepository    repo,
        IVentaRepository      ventaRepo,
        IReparacionRepository repRepo,
        IPdfService           pdfService,
        IMapper               mapper)
    {
        _repo       = repo;
        _ventaRepo  = ventaRepo;
        _repRepo    = repRepo;
        _pdfService = pdfService;
        _mapper     = mapper;
    }

    public async Task<IEnumerable<FacturaDto>> GetAllAsync()
    {
        var facturas = await _repo.GetAllAsync();
        return _mapper.Map<IEnumerable<FacturaDto>>(facturas);
    }

    public async Task<FacturaDto?> GetByIdAsync(int id)
    {
        var factura = await _repo.GetByIdAsync(id);
        return factura == null ? null : _mapper.Map<FacturaDto>(factura);
    }

    public async Task<byte[]> DescargarPdfAsync(int id)
    {
        var factura = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Factura {id} no encontrada.");

        // 1. Si el archivo ya existe en disco, devolverlo directamente
        if (!string.IsNullOrEmpty(factura.PdfPath) && File.Exists(factura.PdfPath))
            return await File.ReadAllBytesAsync(factura.PdfPath);

        // 2. El archivo no existe: regenerar on-the-fly desde los datos fuente
        var facturaDto          = _mapper.Map<FacturaDto>(factura);
        facturaDto.Lineas       = await ObtenerLineasAsync(factura);

        var pdfPath = await _pdfService.GenerarFacturaPdfAsync(facturaDto);
        await _repo.UpdatePdfPathAsync(id, pdfPath);
        return await File.ReadAllBytesAsync(pdfPath);
    }

    public async Task AnularAsync(int id, string motivo)
    {
        _ = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Factura {id} no encontrada.");

        await _repo.AnularAsync(id, motivo);
    }

    public async Task<CrearFacturaResponseDto> CreateManualAsync(CreateFacturaDto dto)
    {
        var baseImp = dto.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario);
        var iva     = Math.Round(baseImp * 0.21m, 2);
        var total   = baseImp + iva;

        var factura = new Models.Factura
        {
            ClienteId    = dto.ClienteId,
            FechaEmision = dto.FechaEmision.Date,
            BaseImponible = baseImp,
            PorcentajeIva = 21,
            ImporteIva    = iva,
            Total         = total
        };

        var id = await _repo.CreateAsync(factura);

        // Enriquecer el DTO con las líneas para generar el PDF ahora
        var facturaDto = await GetByIdAsync(id)
            ?? throw new InvalidOperationException("No se pudo recuperar la factura recién creada.");

        facturaDto.Lineas = dto.Lineas.Select(l => new FacturaLineaDto
        {
            Descripcion    = l.Descripcion,
            Cantidad       = l.Cantidad,
            PrecioUnitario = l.PrecioUnitario,
            Subtotal       = l.Cantidad * l.PrecioUnitario
        }).ToList();

        var pdfPath = await _pdfService.GenerarFacturaPdfAsync(facturaDto);
        await _repo.UpdatePdfPathAsync(id, pdfPath);

        return new CrearFacturaResponseDto { Id = id, NumeroFactura = facturaDto.NumeroFactura };
    }

    // ── Privado ──────────────────────────────────────────────────────

    /// <summary>
    /// Reconstruye las líneas de la factura desde la venta o reparación de origen.
    /// </summary>
    private async Task<List<FacturaLineaDto>> ObtenerLineasAsync(Models.Factura factura)
    {
        // Origen: venta
        if (factura.VentaId.HasValue)
        {
            var venta = await _ventaRepo.GetByIdAsync(factura.VentaId.Value);
            if (venta?.Detalles.Any() == true)
            {
                return venta.Detalles.Select(d => new FacturaLineaDto
                {
                    Descripcion    = d.Descripcion,
                    Cantidad       = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal       = d.Subtotal
                }).ToList();
            }
        }

        // Origen: reparación
        if (factura.ReparacionId.HasValue)
        {
            var rep = await _repRepo.GetByIdAsync(factura.ReparacionId.Value);
            if (rep != null)
            {
                if (rep.Detalles.Any())
                {
                    return rep.Detalles.Select(d => new FacturaLineaDto
                    {
                        Descripcion    = d.Descripcion,
                        Cantidad       = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal       = d.Subtotal
                    }).ToList();
                }

                // Sin detalles: línea genérica de reparación
                return new List<FacturaLineaDto>
                {
                    new FacturaLineaDto
                    {
                        Descripcion    = $"Reparación {rep.Dispositivo} {rep.Marca} {rep.Modelo}".Trim(),
                        Cantidad       = 1,
                        PrecioUnitario = factura.BaseImponible,
                        Subtotal       = factura.BaseImponible
                    }
                };
            }
        }

        // Fallback genérico
        return new List<FacturaLineaDto>
        {
            new FacturaLineaDto
            {
                Descripcion    = "Servicio",
                Cantidad       = 1,
                PrecioUnitario = factura.BaseImponible,
                Subtotal       = factura.BaseImponible
            }
        };
    }
}
