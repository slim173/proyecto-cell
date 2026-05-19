using CellApi.DTOs;

namespace CellApi.Services;

public interface IInventarioService
{
    Task<IEnumerable<InventarioMovimientoDto>> GetKardexAsync(int? productoId, DateTime? desde, DateTime? hasta);
    Task<IEnumerable<ProductoStockBajoDto>> GetStockBajoAsync();
}
