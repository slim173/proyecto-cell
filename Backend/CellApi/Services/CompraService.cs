using AutoMapper;
using CellApi.DTOs;
using CellApi.Models;
using CellApi.Repositories;

namespace CellApi.Services;

public class CompraService : ICompraService
{
    private readonly ICompraRepository _repo;
    private readonly IMapper _mapper;

    public CompraService(ICompraRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CompraDto>> GetAllAsync()
    {
        var compras = await _repo.GetAllAsync();
        return _mapper.Map<IEnumerable<CompraDto>>(compras);
    }

    public async Task<CompraDto?> GetByIdAsync(int id)
    {
        var compra = await _repo.GetByIdAsync(id);
        return compra == null ? null : _mapper.Map<CompraDto>(compra);
    }

    public async Task<CompraDto> CreateAsync(CreateCompraDto dto)
    {
        _ = await _repo.GetProveedorByIdAsync(dto.ProveedorId)
            ?? throw new KeyNotFoundException($"Proveedor {dto.ProveedorId} no encontrado.");

        var detalles = dto.Detalles.Select(d => new CompraDetalle
        {
            ProductoId    = d.ProductoId,
            Cantidad      = d.Cantidad,
            CostoUnitario = d.CostoUnitario,
            Subtotal      = Math.Round(d.Cantidad * d.CostoUnitario, 2)
        }).ToList();

        var compra = new Compra
        {
            ProveedorId   = dto.ProveedorId,
            Total         = detalles.Sum(d => d.Subtotal),
            Estado        = "pendiente",
            Observaciones = dto.Observaciones
        };

        var creada = await _repo.CreateAsync(compra, detalles);
        return _mapper.Map<CompraDto>(creada);
    }

    public async Task<IEnumerable<ProveedorDto>> GetProveedoresAsync(bool soloActivos = true)
    {
        var proveedores = await _repo.GetProveedoresAsync(soloActivos);
        return _mapper.Map<IEnumerable<ProveedorDto>>(proveedores);
    }

    public async Task<ProveedorDto> CreateProveedorAsync(CreateProveedorDto dto)
    {
        var proveedor = _mapper.Map<Proveedor>(dto);
        proveedor.Activo = true;
        var id = await _repo.CreateProveedorAsync(proveedor);
        proveedor.Id = id;
        return _mapper.Map<ProveedorDto>(proveedor);
    }

    public async Task<ProveedorDto> UpdateProveedorAsync(int id, UpdateProveedorDto dto)
    {
        var existente = await _repo.GetProveedorByIdAsync(id)
            ?? throw new KeyNotFoundException($"Proveedor {id} no encontrado.");

        _mapper.Map(dto, existente);
        existente.Id = id;
        await _repo.UpdateProveedorAsync(existente);
        return _mapper.Map<ProveedorDto>(existente);
    }
}
