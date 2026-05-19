using CellApi.Models;

namespace CellApi.Repositories;

public interface IProductoRepository
{
    Task<IEnumerable<Producto>> GetAllAsync(bool soloActivos = true);
    Task<Producto?> GetByIdAsync(int id);
    Task<int> CreateAsync(Producto producto);
    Task UpdateAsync(Producto producto);
    Task<IEnumerable<Categoria>> GetCategoriasAsync();
    Task<IEnumerable<Producto>> GetStockBajoAsync();
    Task UpdateStockAsync(int productoId, int nuevoStock);
    Task<Producto?> GetByCodigoAsync(string q);
}
