using CellApi.Models;

namespace CellApi.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByUsernameAsync(string username);
    Task UpdatePerfilAsync(string username, string nombre, string? email, string? passwordHash);
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<int> CreateAsync(Usuario usuario);
    Task UpdateAdminAsync(Usuario usuario);
}
