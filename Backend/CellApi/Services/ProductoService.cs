using AutoMapper;
using CellApi.DTOs;
using CellApi.Models;
using CellApi.Repositories;

namespace CellApi.Services;

public class ProductoService : IProductoService
{
    private readonly IProductoRepository _repo;
    private readonly IInventarioRepository _invRepo;
    private readonly IMapper _mapper;

    public ProductoService(IProductoRepository repo, IInventarioRepository invRepo, IMapper mapper)
    {
        _repo = repo;
        _invRepo = invRepo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductoDto>> GetAllAsync(bool soloActivos = true)
    {
        var productos = await _repo.GetAllAsync(soloActivos);
        return _mapper.Map<IEnumerable<ProductoDto>>(productos);
    }

    public async Task<ProductoDto?> GetByIdAsync(int id)
    {
        var producto = await _repo.GetByIdAsync(id);
        return producto == null ? null : _mapper.Map<ProductoDto>(producto);
    }

    public async Task<ProductoDto> CreateAsync(CreateProductoDto dto)
    {
        var producto = _mapper.Map<Producto>(dto);
        producto.Activo = true;

        var id = await _repo.CreateAsync(producto);
        producto.Id = id;

        if (dto.Stock > 0)
        {
            await _invRepo.CreateMovimientoAsync(new InventarioMovimiento
            {
                ProductoId     = id,
                Tipo           = "entrada",
                Cantidad       = dto.Stock,
                StockAnterior  = 0,
                StockPosterior = dto.Stock,
                ReferenciaTipo = "ajuste",
                Observaciones  = "Stock inicial al crear producto",
                Usuario        = "sistema"
            });
        }

        return _mapper.Map<ProductoDto>(producto);
    }

    public async Task<ProductoDto> UpdateAsync(int id, UpdateProductoDto dto)
    {
        var existente = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Producto {id} no encontrado.");

        _mapper.Map(dto, existente);
        existente.Id = id;

        await _repo.UpdateAsync(existente);
        return _mapper.Map<ProductoDto>(existente);
    }

    public async Task DeleteAsync(int id)
    {
        var existente = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Producto {id} no encontrado.");

        existente.Activo = false;
        await _repo.UpdateAsync(existente);
    }

    public async Task<IEnumerable<CategoriaDto>> GetCategoriasAsync()
    {
        var cats = await _repo.GetCategoriasAsync();
        return _mapper.Map<IEnumerable<CategoriaDto>>(cats);
    }

    public async Task<IEnumerable<ProductoDto>> GetStockBajoAsync()
    {
        var productos = await _repo.GetStockBajoAsync();
        return _mapper.Map<IEnumerable<ProductoDto>>(productos);
    }

    public async Task AjustarStockAsync(AjusteStockDto dto)
    {
        var producto = await _repo.GetByIdAsync(dto.ProductoId)
            ?? throw new KeyNotFoundException($"Producto {dto.ProductoId} no encontrado.");

        var stockAnterior = producto.Stock;
        await _repo.UpdateStockAsync(dto.ProductoId, dto.NuevoStock);

        var diferencia = dto.NuevoStock - stockAnterior;
        await _invRepo.CreateMovimientoAsync(new InventarioMovimiento
        {
            ProductoId     = dto.ProductoId,
            Tipo           = "ajuste",
            Cantidad       = Math.Abs(diferencia),
            StockAnterior  = stockAnterior,
            StockPosterior = dto.NuevoStock,
            ReferenciaTipo = "ajuste",
            Observaciones  = dto.Observaciones ?? "Ajuste manual de stock",
            Usuario        = "sistema"
        });
    }

    public async Task<ProductoDto?> GetByCodigoAsync(string q)
    {
        var p = await _repo.GetByCodigoAsync(q);
        return p == null ? null : _mapper.Map<ProductoDto>(p);
    }
}
