using CellApi.Models;

namespace CellApi.Repositories;

public interface IEmailLogRepository
{
    Task<int> CreateAsync(EmailLog log);
    Task UpdateEstadoAsync(int id, string estado, string? errorMensaje, DateTime? fechaEnvio);
    Task<IEnumerable<EmailLog>> GetByReferenciaAsync(string tipo, int id);
    Task<IEnumerable<EmailLog>> GetByTipoAsync(string tipo, int limit = 50);
}
