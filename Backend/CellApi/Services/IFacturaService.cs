using CellApi.DTOs;

namespace CellApi.Services;

public interface IFacturaService
{
    Task<IEnumerable<FacturaDto>> GetAllAsync();
    Task<FacturaDto?> GetByIdAsync(int id);
    Task<byte[]> DescargarPdfAsync(int id, string? formato = null);
    Task AnularAsync(int id, string motivo);
    Task<CrearFacturaResponseDto> CreateManualAsync(CreateFacturaDto dto);
}
