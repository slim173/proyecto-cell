using CellApi.DTOs;

namespace CellApi.Services;

public interface ICompraService
{
    Task<IEnumerable<CompraDto>> GetAllAsync();
    Task<CompraDto?> GetByIdAsync(int id);
    Task<CompraDto> CreateAsync(CreateCompraDto dto);
    Task<IEnumerable<ProveedorDto>> GetProveedoresAsync(bool soloActivos = true);
    Task<ProveedorDto> CreateProveedorAsync(CreateProveedorDto dto);
    Task<ProveedorDto> UpdateProveedorAsync(int id, UpdateProveedorDto dto);
}
