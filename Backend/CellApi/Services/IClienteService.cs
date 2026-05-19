using CellApi.DTOs;

namespace CellApi.Services;

public interface IClienteService
{
    Task<IEnumerable<ClienteDto>> GetAllAsync(bool soloActivos = true);
    Task<ClienteDto?> GetByIdAsync(int id);
    Task<ClienteDto> CreateAsync(CreateClienteDto dto);
    Task<ClienteDto> UpdateAsync(int id, UpdateClienteDto dto);
    Task DeleteAsync(int id);
}
