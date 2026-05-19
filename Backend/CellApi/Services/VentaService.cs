using AutoMapper;
using CellApi.DTOs;
using CellApi.Models;
using CellApi.Repositories;

namespace CellApi.Services;

public class VentaService : IVentaService
{
    private readonly IVentaRepository     _ventaRepo;
    private readonly IClienteRepository   _clienteRepo;
    private readonly IProductoRepository  _productoRepo;
    private readonly IFacturaRepository   _facturaRepo;
    private readonly IEmailService        _emailService;
    private readonly IPdfService          _pdfService;
    private readonly IMapper              _mapper;

    public VentaService(
        IVentaRepository ventaRepo,
        IClienteRepository clienteRepo,
        IProductoRepository productoRepo,
        IFacturaRepository facturaRepo,
        IEmailService emailService,
        IPdfService pdfService,
        IMapper mapper)
    {
        _ventaRepo    = ventaRepo;
        _clienteRepo  = clienteRepo;
        _productoRepo = productoRepo;
        _facturaRepo  = facturaRepo;
        _emailService = emailService;
        _pdfService   = pdfService;
        _mapper       = mapper;
    }

    public async Task<IEnumerable<VentaDto>> GetAllAsync()
    {
        var ventas = await _ventaRepo.GetAllAsync();
        return _mapper.Map<IEnumerable<VentaDto>>(ventas);
    }

    public async Task<VentaDto?> GetByIdAsync(int id)
    {
        var venta = await _ventaRepo.GetByIdAsync(id);
        return venta == null ? null : _mapper.Map<VentaDto>(venta);
    }

    public async Task<VentaDto> CreateAsync(CreateVentaDto dto)
    {
        // 1. Validar cliente
        var cliente = await _clienteRepo.GetByIdAsync(dto.ClienteId)
            ?? throw new KeyNotFoundException($"Cliente {dto.ClienteId} no encontrado.");

        // 2. Construir detalles y calcular importes
        const decimal porcentajeIva = 21m;
        var detalles = new List<VentaDetalle>();

        foreach (var item in dto.Detalles)
        {
            if (item.ProductoId.HasValue)
            {
                var producto = await _productoRepo.GetByIdAsync(item.ProductoId.Value)
                    ?? throw new KeyNotFoundException($"Producto {item.ProductoId} no encontrado.");

                if (producto.Stock < item.Cantidad)
                    throw new InvalidOperationException(
                        $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}, solicitado: {item.Cantidad}.");
            }

            detalles.Add(new VentaDetalle
            {
                ProductoId     = item.ProductoId,
                Descripcion    = item.Descripcion,
                Cantidad       = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                Subtotal       = Math.Round(item.Cantidad * item.PrecioUnitario, 2)
            });
        }

        var subtotalLineas = detalles.Sum(d => d.Subtotal);
        var descuento      = dto.Descuento > 0 ? dto.Descuento : 0m;
        var descuentoReal  = dto.TipoDescuento == "porcentaje"
            ? Math.Round(subtotalLineas * descuento / 100, 2)
            : Math.Round(descuento, 2);
        var baseImponible  = Math.Round(subtotalLineas - descuentoReal, 2);
        var importeIva     = Math.Round(baseImponible * porcentajeIva / 100, 2);
        var total          = baseImponible + importeIva;

        var venta = new Venta
        {
            ClienteId      = dto.ClienteId,
            BaseImponible  = baseImponible,
            PorcentajeIva  = porcentajeIva,
            ImporteIva     = importeIva,
            Descuento      = descuentoReal,
            TipoDescuento  = dto.TipoDescuento,
            Estado         = "cobrada",
            MetodoPago     = dto.MetodoPago,
            Observaciones  = dto.Observaciones,
            Total          = total
        };

        // 3. Persistir (transacción interna en repo: venta + detalles + stock + kardex)
        var ventaCreada = await _ventaRepo.CreateAsync(venta, detalles);

        // 4. Crear factura + PDF
        await CrearFacturaParaVentaAsync(ventaCreada, cliente, detalles, baseImponible, porcentajeIva, importeIva, total);

        return _mapper.Map<VentaDto>(ventaCreada);
    }

    public async Task UpdateEstadoAsync(int id, string estado)
    {
        var estadosValidos = new[] { "pendiente", "cobrada", "anulada" };
        if (!estadosValidos.Contains(estado))
            throw new ArgumentException($"Estado '{estado}' no válido.");

        _ = await _ventaRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Venta {id} no encontrada.");

        await _ventaRepo.UpdateEstadoAsync(id, estado);
    }

    public async Task EnviarFacturaAsync(int id)
    {
        var venta = await _ventaRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Venta {id} no encontrada.");

        var factura = await _facturaRepo.GetByVentaIdAsync(id)
            ?? throw new InvalidOperationException($"No existe factura para la venta {id}.");

        byte[]? pdfBytes = null;
        string? nombrePdf = null;

        if (factura.PdfPath != null && File.Exists(factura.PdfPath))
        {
            pdfBytes  = await File.ReadAllBytesAsync(factura.PdfPath);
            nombrePdf = Path.GetFileName(factura.PdfPath);
        }

        var asunto = $"Factura {factura.NumeroFactura} - CellShop";
        var cuerpo = $@"
            <h2>Gracias por su compra, {venta.ClienteNombre}</h2>
            <p>Adjuntamos la factura <strong>{factura.NumeroFactura}</strong> correspondiente a su compra del {venta.Fecha:dd/MM/yyyy}.</p>
            <table>
                <tr><td><strong>Base imponible:</strong></td><td>{factura.BaseImponible:F2} €</td></tr>
                <tr><td><strong>IVA ({factura.PorcentajeIva}%):</strong></td><td>{factura.ImporteIva:F2} €</td></tr>
                <tr><td><strong>Total:</strong></td><td><strong>{factura.Total:F2} €</strong></td></tr>
            </table>
            <p>Un saludo,<br/>CellShop</p>";

        await _emailService.SendAsync(
            venta.ClienteEmail!,
            asunto,
            cuerpo,
            "factura_venta",
            "venta",
            id,
            pdfBytes,
            nombrePdf);

        await _ventaRepo.MarcarFacturaEnviadaAsync(id);
    }

    public async Task GenerarFacturaPendienteAsync(int id)
    {
        var venta = await _ventaRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Venta {id} no encontrada.");

        // Verificar que no tenga ya factura
        var existing = await _facturaRepo.GetByVentaIdAsync(id);
        if (existing != null)
            throw new InvalidOperationException("Esta venta ya tiene una factura generada.");

        var cliente = await _clienteRepo.GetByIdAsync(venta.ClienteId)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // Reconstruir líneas desde los detalles ya guardados
        var detalles = venta.Detalles.Select(d => new VentaDetalle
        {
            ProductoId     = d.ProductoId,
            Descripcion    = d.Descripcion,
            Cantidad       = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal       = d.Subtotal
        }).ToList();

        await CrearFacturaParaVentaAsync(
            venta, cliente, detalles,
            venta.BaseImponible, venta.PorcentajeIva, venta.ImporteIva, venta.Total);
    }

    // ── Helper compartido ─────────────────────────────────────────
    private async Task CrearFacturaParaVentaAsync(
        Venta venta, Models.Cliente cliente, List<VentaDetalle> detalles,
        decimal baseImponible, decimal porcentajeIva, decimal importeIva, decimal total)
    {
        var factura = new Factura
        {
            VentaId          = venta.Id,
            ClienteId        = venta.ClienteId,
            FechaEmision     = DateTime.Today,
            BaseImponible    = baseImponible,
            PorcentajeIva    = porcentajeIva,
            ImporteIva       = importeIva,
            Total            = total,
            ClienteNombre    = cliente.Nombre,
            ClienteApellidos = cliente.Apellidos,
            ClienteEmail     = cliente.Email,
            ClienteNif       = cliente.Nif,
            ClienteDireccion = cliente.Direccion
        };

        var facturaId = await _facturaRepo.CreateAsync(factura);
        factura.Id = facturaId;

        var lineas = detalles.Select(d => new FacturaLineaDto
        {
            Descripcion    = d.Descripcion,
            Cantidad       = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal       = d.Subtotal
        }).ToList();

        var facturaDto = _mapper.Map<FacturaDto>(factura);
        facturaDto.Lineas = lineas;

        var pdfPath = await _pdfService.GenerarFacturaPdfAsync(facturaDto);
        await _facturaRepo.UpdatePdfPathAsync(facturaId, pdfPath);
    }
}
