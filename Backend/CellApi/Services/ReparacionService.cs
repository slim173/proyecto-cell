using AutoMapper;
using CellApi.DTOs;
using CellApi.Models;
using CellApi.Repositories;

namespace CellApi.Services;

public class ReparacionService : IReparacionService
{
    private readonly IReparacionRepository _repo;
    private readonly IClienteRepository   _clienteRepo;
    private readonly IProductoRepository  _productoRepo;
    private readonly IInventarioRepository _invRepo;
    private readonly IFacturaRepository   _facturaRepo;
    private readonly IEmailService        _emailService;
    private readonly IPdfService          _pdfService;
    private readonly IMapper              _mapper;

    public ReparacionService(
        IReparacionRepository repo,
        IClienteRepository clienteRepo,
        IProductoRepository productoRepo,
        IInventarioRepository invRepo,
        IFacturaRepository facturaRepo,
        IEmailService emailService,
        IPdfService pdfService,
        IMapper mapper)
    {
        _repo         = repo;
        _clienteRepo  = clienteRepo;
        _productoRepo = productoRepo;
        _invRepo      = invRepo;
        _facturaRepo  = facturaRepo;
        _emailService = emailService;
        _pdfService   = pdfService;
        _mapper       = mapper;
    }

    public async Task<IEnumerable<ReparacionDto>> GetAllAsync()
    {
        var reparaciones = await _repo.GetAllAsync();
        return _mapper.Map<IEnumerable<ReparacionDto>>(reparaciones);
    }

    public async Task<ReparacionDto?> GetByIdAsync(int id)
    {
        var rep = await _repo.GetByIdAsync(id);
        return rep == null ? null : _mapper.Map<ReparacionDto>(rep);
    }

    public async Task<ReparacionDto> CreateAsync(CreateReparacionDto dto)
    {
        var cliente = await _clienteRepo.GetByIdAsync(dto.ClienteId)
            ?? throw new KeyNotFoundException($"Cliente {dto.ClienteId} no encontrado.");

        var prioridadesValidas = new[] { "baja", "normal", "alta", "urgente" };
        if (!prioridadesValidas.Contains(dto.Prioridad))
            throw new ArgumentException($"Prioridad '{dto.Prioridad}' no válida.");

        var reparacion = new Reparacion
        {
            ClienteId             = dto.ClienteId,
            Dispositivo           = dto.Dispositivo,
            Marca                 = dto.Marca,
            Modelo                = dto.Modelo,
            Imei                  = dto.Imei,
            DescripcionFalla      = dto.DescripcionFalla,
            Estado                = "recibido",
            Prioridad             = dto.Prioridad,
            PrecioEstimado        = dto.PrecioEstimado,
            TecnicoAsignado       = dto.TecnicoAsignado,
            FechaEstimadaEntrega  = dto.FechaEstimadaEntrega,
            FechaRecepcion        = DateTime.UtcNow
        };

        var id = await _repo.CreateAsync(reparacion);
        reparacion.Id = id;

        // Email de confirmación de recepción
        var asunto = $"Orden de reparación recibida - {reparacion.NumeroOrden}";
        var cuerpo = $@"
            <h2>Hemos recibido su dispositivo, {cliente.Nombre}</h2>
            <p>Su número de orden es: <strong>{reparacion.NumeroOrden}</strong></p>
            <p><strong>Dispositivo:</strong> {reparacion.Dispositivo} {reparacion.Marca} {reparacion.Modelo}</p>
            <p><strong>Descripción de la falla:</strong> {reparacion.DescripcionFalla}</p>
            {(reparacion.PrecioEstimado.HasValue ? $"<p><strong>Presupuesto (IVA incluido):</strong> {reparacion.PrecioEstimado:F2} €</p>" : "")}
            {(reparacion.FechaEstimadaEntrega.HasValue ? $"<p><strong>Fecha estimada de entrega:</strong> {reparacion.FechaEstimadaEntrega:dd/MM/yyyy}</p>" : "")}
            <p>Le mantendremos informado sobre el estado de su reparación.</p>
            <p>Un saludo,<br/>CellShop</p>";

        // Fire-and-forget: no bloquear el registro esperando SMTP
        _ = _emailService.SendAsync(
            cliente.Email ?? "", asunto, cuerpo,
            "recepcion_reparacion", "reparacion", id);

        var resultado = await _repo.GetByIdAsync(id);
        return _mapper.Map<ReparacionDto>(resultado!);
    }

    public async Task<ReparacionDto> UpdateAsync(int id, UpdateReparacionDto dto)
    {
        var rep = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Reparación {id} no encontrada.");

        _ = await _clienteRepo.GetByIdAsync(dto.ClienteId)
            ?? throw new KeyNotFoundException($"Cliente {dto.ClienteId} no encontrado.");

        var prioridadesValidas = new[] { "baja", "normal", "alta", "urgente" };
        if (!prioridadesValidas.Contains(dto.Prioridad))
            throw new ArgumentException($"Prioridad '{dto.Prioridad}' no válida.");

        rep.ClienteId            = dto.ClienteId;
        rep.Dispositivo          = dto.Dispositivo;
        rep.Marca                = dto.Marca;
        rep.Modelo               = dto.Modelo;
        rep.Imei                 = dto.Imei;
        rep.DescripcionFalla     = dto.DescripcionFalla;
        rep.Prioridad            = dto.Prioridad;
        rep.PrecioEstimado       = dto.PrecioEstimado;
        rep.TecnicoAsignado      = dto.TecnicoAsignado;
        rep.FechaEstimadaEntrega = dto.FechaEstimadaEntrega;

        await _repo.UpdateAsync(rep);

        var resultado = await _repo.GetByIdAsync(id);
        return _mapper.Map<ReparacionDto>(resultado!);
    }

    public async Task<ReparacionDto> UpdateEstadoAsync(int id, UpdateReparacionEstadoDto dto)
    {
        var rep = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Reparación {id} no encontrada.");

        var estadosValidos = new[] { "recibido", "diagnosticado", "en_reparacion", "reparado", "entregado", "no_reparable" };
        if (!estadosValidos.Contains(dto.Estado))
            throw new ArgumentException($"Estado '{dto.Estado}' no válido.");

        rep.Estado               = dto.Estado;
        rep.ObservacionesTecnico = dto.ObservacionesTecnico ?? rep.ObservacionesTecnico;
        rep.Solucion             = dto.Solucion ?? rep.Solucion;
        rep.PrecioEstimado       = dto.PrecioEstimado ?? rep.PrecioEstimado;
        rep.PrecioFinal          = dto.PrecioFinal ?? rep.PrecioFinal;
        rep.TecnicoAsignado      = dto.TecnicoAsignado ?? rep.TecnicoAsignado;
        rep.FechaEstimadaEntrega = dto.FechaEstimadaEntrega ?? rep.FechaEstimadaEntrega;

        if (dto.Estado == "entregado")
            rep.FechaEntregaReal = DateTime.UtcNow;

        await _repo.UpdateEstadoAsync(rep);

        var cliente = await _clienteRepo.GetByIdAsync(rep.ClienteId);
        // Fire-and-forget: no bloquear el cambio de estado esperando SMTP
        if (cliente != null)
            _ = EnviarEmailCambioEstado(rep, cliente, dto.Estado);

        // Cuando se entrega: PrecioFinal ES el PVP (IVA ya incluido)
        if (dto.Estado == "entregado" && dto.PrecioFinal.HasValue)
        {
            const decimal pct = 21m;
            var total   = Math.Round(dto.PrecioFinal.Value, 2);
            var baseImp = Math.Round(total / (1m + pct / 100m), 2);
            var iva     = Math.Round(total - baseImp, 2);

            await _repo.ActualizarTotalesAsync(id, baseImp, pct, iva, total, dto.PrecioFinal.Value);

            var factura = new Factura
            {
                ReparacionId   = id,
                ClienteId      = rep.ClienteId,
                FechaEmision   = DateTime.Today,
                BaseImponible  = baseImp,
                PorcentajeIva  = pct,
                ImporteIva     = iva,
                Total          = total,
                ClienteNombre  = cliente?.Nombre,
                ClienteApellidos = cliente?.Apellidos,
                ClienteEmail   = cliente?.Email,
                ClienteNif     = cliente?.Nif,
                ClienteDireccion = cliente?.Direccion
            };

            var facturaId = await _facturaRepo.CreateAsync(factura);
            factura.Id = facturaId;

            var lineas = rep.Detalles.Select(d => new FacturaLineaDto
            {
                Descripcion    = d.Descripcion,
                Cantidad       = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal       = d.Subtotal
            }).ToList();

            if (!lineas.Any())
                lineas.Add(new FacturaLineaDto
                {
                    Descripcion    = $"Reparación {rep.Dispositivo} {rep.Marca} {rep.Modelo}",
                    Cantidad       = 1,
                    PrecioUnitario = baseImp,   // base s/IVA; el PDF muestra PVP = base × ivaFactor
                    Subtotal       = baseImp
                });

            var facturaDto = _mapper.Map<FacturaDto>(factura);
            facturaDto.Lineas = lineas;

            var pdfPath = await _pdfService.GenerarFacturaPdfAsync(facturaDto);
            await _facturaRepo.UpdatePdfPathAsync(facturaId, pdfPath);
        }

        var updated = await _repo.GetByIdAsync(id);
        return _mapper.Map<ReparacionDto>(updated!);
    }

    public async Task<ReparacionDetalleDto> AddDetalleAsync(int id, AddReparacionDetalleDto dto)
    {
        var rep = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Reparación {id} no encontrada.");

        if (dto.ProductoId.HasValue)
        {
            var producto = await _productoRepo.GetByIdAsync(dto.ProductoId.Value)
                ?? throw new KeyNotFoundException($"Producto {dto.ProductoId} no encontrado.");

            if (producto.Stock < dto.Cantidad)
                throw new InvalidOperationException(
                    $"Stock insuficiente para '{producto.Nombre}'.");

            var stockActual = producto.Stock;
            var nuevoStock  = stockActual - dto.Cantidad;
            await _productoRepo.UpdateStockAsync(dto.ProductoId.Value, nuevoStock);

            await _invRepo.CreateMovimientoAsync(new InventarioMovimiento
            {
                ProductoId     = dto.ProductoId.Value,
                Tipo           = "salida",
                Cantidad       = dto.Cantidad,
                StockAnterior  = stockActual,
                StockPosterior = nuevoStock,
                ReferenciaTipo = "reparacion",
                ReferenciaId   = id,
                Observaciones  = $"Pieza usada en reparación {rep.NumeroOrden}",
                Usuario        = "sistema"
            });
        }

        var detalle = new ReparacionDetalle
        {
            ReparacionId   = id,
            ProductoId     = dto.ProductoId,
            Descripcion    = dto.Descripcion,
            Cantidad       = dto.Cantidad,
            PrecioUnitario = dto.PrecioUnitario,
            Subtotal       = Math.Round(dto.Cantidad * dto.PrecioUnitario, 2)
        };

        var detalleId = await _repo.AddDetalleAsync(detalle);
        detalle.Id = detalleId;

        return _mapper.Map<ReparacionDetalleDto>(detalle);
    }

    public async Task RemoveDetalleAsync(int reparacionId, int detalleId)
    {
        _ = await _repo.GetByIdAsync(reparacionId)
            ?? throw new KeyNotFoundException($"Reparación {reparacionId} no encontrada.");

        await _repo.RemoveDetalleAsync(detalleId);
    }

    public async Task EnviarNotificacionAsync(int id)
    {
        var rep = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Reparación {id} no encontrada.");

        var cliente = await _clienteRepo.GetByIdAsync(rep.ClienteId)
            ?? throw new InvalidOperationException("Cliente no encontrado.");

        await EnviarEmailCambioEstado(rep, cliente, rep.Estado);
    }

    public async Task<IEnumerable<HistorialEquipoDto>> GetHistorialEquipoAsync(int id)
    {
        var rep = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Reparación {id} no encontrada.");

        if (string.IsNullOrWhiteSpace(rep.Imei))
            return Enumerable.Empty<HistorialEquipoDto>();

        var historial = await _repo.GetHistorialByImeiAsync(rep.Imei, id);
        return historial.Select(r => new HistorialEquipoDto
        {
            Id              = r.Id,
            NumeroOrden     = r.NumeroOrden,
            Estado          = r.Estado,
            FechaRecepcion  = r.FechaRecepcion,
            FechaEntregaReal = r.FechaEntregaReal,
            DescripcionFalla = r.DescripcionFalla,
            Solucion        = r.Solucion,
            TecnicoAsignado = r.TecnicoAsignado,
            Total           = r.Total
        });
    }

    // ── Privado ──────────────────────────────────────────────────
    private async Task EnviarEmailCambioEstado(Reparacion rep, Models.Cliente cliente, string estado)
    {
        string asunto, cuerpo;

        switch (estado)
        {
            case "diagnosticado":
                asunto = $"Diagnóstico completado - Orden {rep.NumeroOrden}";
                cuerpo = $@"
                    <h2>Diagnóstico completado, {cliente.Nombre}</h2>
                    <p>Hemos terminado el diagnóstico de su <strong>{rep.Dispositivo} {rep.Marca} {rep.Modelo}</strong>.</p>
                    {(rep.PrecioEstimado.HasValue ? $"<p><strong>Presupuesto (IVA incluido):</strong> {rep.PrecioEstimado:F2} €</p>" : "")}
                    {(rep.ObservacionesTecnico != null ? $"<p><strong>Observaciones:</strong> {rep.ObservacionesTecnico}</p>" : "")}
                    <p>Un saludo,<br/>CellShop</p>";
                break;

            case "reparado":
                asunto = $"Su dispositivo está listo - Orden {rep.NumeroOrden}";
                cuerpo = $@"
                    <h2>¡Su dispositivo está reparado, {cliente.Nombre}!</h2>
                    <p>Su <strong>{rep.Dispositivo} {rep.Marca} {rep.Modelo}</strong> ya está listo para recoger.</p>
                    {(rep.PrecioFinal.HasValue ? $"<p><strong>Total a pagar (IVA incluido):</strong> {rep.PrecioFinal:F2} €</p>" : "")}
                    <p>Puede pasar a recogerlo en nuestras instalaciones en horario comercial.</p>
                    <p>Un saludo,<br/>CellShop</p>";
                break;

            case "no_reparable":
                asunto = $"Actualización de su reparación - Orden {rep.NumeroOrden}";
                cuerpo = $@"
                    <h2>Información sobre su reparación, {cliente.Nombre}</h2>
                    <p>Lamentamos informarle que su <strong>{rep.Dispositivo} {rep.Marca} {rep.Modelo}</strong> no ha podido ser reparado.</p>
                    {(rep.ObservacionesTecnico != null ? $"<p><strong>Motivo:</strong> {rep.ObservacionesTecnico}</p>" : "")}
                    <p>Puede pasar a recoger su dispositivo en nuestras instalaciones.</p>
                    <p>Un saludo,<br/>CellShop</p>";
                break;

            default:
                return; // Sin email para otros estados
        }

        if (string.IsNullOrWhiteSpace(cliente.Email)) return;
        await _emailService.SendAsync(
            cliente.Email, asunto, cuerpo,
            $"reparacion_{estado}", "reparacion", rep.Id);
    }

    public async Task<ReparacionDto?> GetByNumeroOrdenAsync(string numeroOrden)
    {
        var rep = await _repo.GetByNumeroOrdenAsync(numeroOrden);
        return rep == null ? null : _mapper.Map<ReparacionDto>(rep);
    }

    public async Task<IEnumerable<ReparacionDto>> GetReparadasSinRecogerAsync(int diasLimite)
    {
        var reps = await _repo.GetReparadasSinRecogerAsync(diasLimite);
        return _mapper.Map<IEnumerable<ReparacionDto>>(reps);
    }

    public Task MarcarRecordatorioEnviadoAsync(int id)
        => _repo.MarcarRecordatorioEnviadoAsync(id);
}
