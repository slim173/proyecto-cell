using AutoMapper;
using CellApi.DTOs;
using CellApi.Repositories;

namespace CellApi.Services;

public class InventarioService : IInventarioService
{
    private readonly IInventarioRepository _invRepo;
    private readonly IProductoRepository   _prodRepo;
    private readonly IMapper _mapper;

    public InventarioService(
        IInventarioRepository invRepo,
        IProductoRepository prodRepo,
        IMapper mapper)
    {
        _invRepo  = invRepo;
        _prodRepo = prodRepo;
        _mapper   = mapper;
    }

    public async Task<IEnumerable<InventarioMovimientoDto>> GetKardexAsync(
        int? productoId, DateTime? desde, DateTime? hasta)
    {
        var movimientos = await _invRepo.GetKardexAsync(productoId, desde, hasta);
        return _mapper.Map<IEnumerable<InventarioMovimientoDto>>(movimientos);
    }

    public async Task<IEnumerable<ProductoStockBajoDto>> GetStockBajoAsync()
    {
        var productos = await _prodRepo.GetStockBajoAsync();
        return productos.Select(p => new ProductoStockBajoDto
        {
            Id               = p.Id,
            Codigo           = p.Codigo,
            Nombre           = p.Nombre,
            Stock            = p.Stock,
            StockMinimo      = p.StockMinimo,
            UnidadesFaltantes= p.StockMinimo - p.Stock,
            Categoria        = p.CategoriaNombre
        });
    }
}
