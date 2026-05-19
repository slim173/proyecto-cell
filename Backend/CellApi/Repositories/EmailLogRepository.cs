using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public class EmailLogRepository : IEmailLogRepository
{
    private readonly DbConnectionFactory _db;

    public EmailLogRepository(DbConnectionFactory db) => _db = db;

    public async Task<int> CreateAsync(EmailLog log)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            INSERT INTO email_logs
                (destinatario, asunto, cuerpo, tipo, referencia_tipo, referencia_id, estado, intentos)
            VALUES
                (@Destinatario, @Asunto, @Cuerpo, @Tipo, @ReferenciaTipo, @ReferenciaId, @Estado, @Intentos)
            RETURNING id";
        return await conn.ExecuteScalarAsync<int>(sql, log);
    }

    public async Task UpdateEstadoAsync(int id, string estado, string? errorMensaje, DateTime? fechaEnvio)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE email_logs SET
                estado         = @Estado,
                error_mensaje  = @ErrorMensaje,
                fecha_envio    = @FechaEnvio,
                intentos       = intentos + 1
            WHERE id = @Id",
            new { Id = id, Estado = estado, ErrorMensaje = errorMensaje, FechaEnvio = fechaEnvio });
    }

    public async Task<IEnumerable<EmailLog>> GetByReferenciaAsync(string tipo, int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<EmailLog>(
            "SELECT * FROM email_logs WHERE referencia_tipo = @Tipo AND referencia_id = @Id ORDER BY fecha_creacion DESC",
            new { Tipo = tipo, Id = id });
    }

    public async Task<IEnumerable<EmailLog>> GetByTipoAsync(string tipo, int limit = 50)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<EmailLog>(
            "SELECT * FROM email_logs WHERE tipo = @Tipo ORDER BY fecha_creacion DESC LIMIT @Limit",
            new { Tipo = tipo, Limit = limit });
    }
}
