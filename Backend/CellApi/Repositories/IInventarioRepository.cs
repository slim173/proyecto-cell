using CellApi.Models;

namespace CellApi.Repositories;

public interface IInventarioRepository
{
    Task<IEnumerable<InventarioMovimiento>> GetKardexAsync(int? productoId, DateTime? desde, DateTime? hasta);
    Task<int> CreateMovimientoAsync(InventarioMovimiento movimiento);
}
