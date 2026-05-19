using CellApi.Models;

namespace CellApi.Repositories;

public interface IClienteRepository
{
    Task<IEnumerable<Cliente>> GetAllAsync(bool soloActivos = true);
    Task<Cliente?> GetByIdAsync(int id);
    Task<int> CreateAsync(Cliente cliente);
    Task UpdateAsync(Cliente cliente);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
}
