using CellApi.Models;

namespace CellApi.Repositories;

public interface ICompraRepository
{
    Task<IEnumerable<Compra>> GetAllAsync();
    Task<Compra?> GetByIdAsync(int id);
    Task<Compra> CreateAsync(Compra compra, List<CompraDetalle> detalles);
    Task<IEnumerable<Proveedor>> GetProveedoresAsync(bool soloActivos = true);
    Task<int> CreateProveedorAsync(Proveedor proveedor);
    Task UpdateProveedorAsync(Proveedor proveedor);
    Task<Proveedor?> GetProveedorByIdAsync(int id);
}
