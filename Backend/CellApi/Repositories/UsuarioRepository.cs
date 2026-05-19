using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly DbConnectionFactory _db;
    public UsuarioRepository(DbConnectionFactory db) => _db = db;

    public async Task<Usuario?> GetByUsernameAsync(string username)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Usuario>(
            "SELECT * FROM usuarios WHERE username = @Username AND activo = true",
            new { Username = username });
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Usuario>(
            "SELECT * FROM usuarios ORDER BY fecha_creacion");
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Usuario>(
            "SELECT * FROM usuarios WHERE id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Usuario u)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO usuarios (nombre, username, password_hash, email, rol, activo)
            VALUES (@Nombre, @Username, @PasswordHash, @Email, @Rol, @Activo)
            RETURNING id", u);
    }

    public async Task UpdateAdminAsync(Usuario u)
    {
        using var conn = _db.CreateConnection();
        if (!string.IsNullOrEmpty(u.PasswordHash))
        {
            await conn.ExecuteAsync(@"
                UPDATE usuarios SET nombre = @Nombre, email = @Email, rol = @Rol,
                    activo = @Activo, password_hash = @PasswordHash
                WHERE id = @Id", u);
        }
        else
        {
            await conn.ExecuteAsync(@"
                UPDATE usuarios SET nombre = @Nombre, email = @Email, rol = @Rol,
                    activo = @Activo
                WHERE id = @Id", u);
        }
    }

    public async Task UpdatePerfilAsync(string username, string nombre, string? email, string? passwordHash)
    {
        using var conn = _db.CreateConnection();
        if (passwordHash != null)
        {
            await conn.ExecuteAsync(
                "UPDATE usuarios SET nombre = @Nombre, email = @Email, password_hash = @PasswordHash WHERE username = @Username",
                new { Nombre = nombre, Email = email, PasswordHash = passwordHash, Username = username });
        }
        else
        {
            await conn.ExecuteAsync(
                "UPDATE usuarios SET nombre = @Nombre, email = @Email WHERE username = @Username",
                new { Nombre = nombre, Email = email, Username = username });
        }
    }
}
