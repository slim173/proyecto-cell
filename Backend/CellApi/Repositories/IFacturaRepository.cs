using CellApi.Models;

namespace CellApi.Repositories;

public interface IFacturaRepository
{
    Task<IEnumerable<Factura>> GetAllAsync();
    Task<Factura?> GetByIdAsync(int id);
    Task<Factura?> GetByVentaIdAsync(int ventaId);
    Task<Factura?> GetByReparacionIdAsync(int reparacionId);
    Task<int> CreateAsync(Factura factura);
    Task UpdatePdfPathAsync(int id, string pdfPath);
    Task AnularAsync(int id, string motivo);
}
