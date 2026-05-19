using CellApi.DTOs;

namespace CellApi.Services;

public interface IFacturaService
{
    Task<IEnumerable<FacturaDto>> GetAllAsync();
    Task<FacturaDto?> GetByIdAsync(int id);
    Task<byte[]> DescargarPdfAsync(int id);
    Task AnularAsync(int id, string motivo);
    Task<CrearFacturaResponseDto> CreateManualAsync(CreateFacturaDto dto);
}
