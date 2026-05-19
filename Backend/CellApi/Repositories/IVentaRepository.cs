using CellApi.Models;

namespace CellApi.Repositories;

public interface IVentaRepository
{
    Task<IEnumerable<Venta>> GetAllAsync();
    Task<Venta?> GetByIdAsync(int id);
    Task<Venta> CreateAsync(Venta venta, List<VentaDetalle> detalles);
    Task UpdateEstadoAsync(int id, string estado);
    Task MarcarFacturaEnviadaAsync(int id);
}
