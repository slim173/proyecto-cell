using CellApi.Models;

namespace CellApi.Repositories;

public interface IConfiguracionRepository
{
    Task<string?> GetValorAsync(string clave);
    Task<Dictionary<string, string>> GetAllAsync();
    Task SetValorAsync(string clave, string valor);
    Task SetMultipleAsync(Dictionary<string, string> valores);
}
