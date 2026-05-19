using CellApi.DTOs;

namespace CellApi.Services;

public interface IVentaService
{
    Task<IEnumerable<VentaDto>> GetAllAsync();
    Task<VentaDto?> GetByIdAsync(int id);
    Task<VentaDto> CreateAsync(CreateVentaDto dto);
    Task UpdateEstadoAsync(int id, string estado);
    Task EnviarFacturaAsync(int id);
    Task GenerarFacturaPendienteAsync(int id);
}
