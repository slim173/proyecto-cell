using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CellApi.DTOs;
using CellApi.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CellApi.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _repo;
    private readonly IConfiguration _config;

    public AuthService(IUsuarioRepository repo, IConfiguration config)
    {
        _repo   = repo;
        _config = config;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var usuario = await _repo.GetByUsernameAsync(dto.Username);
        if (usuario == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash)) return null;

        var expiracion = DateTime.UtcNow.AddHours(
            _config.GetValue<int>("Jwt:ExpirationHours", 10));

        return new AuthResponseDto
        {
            Token      = GenerarJwt(usuario.Username, usuario.Nombre, usuario.Rol),
            Username   = usuario.Username,
            Nombre     = usuario.Nombre,
            Rol        = usuario.Rol,
            Expiration = expiracion
        };
    }

    private string GenerarJwt(string username, string nombre, string rol)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var exp   = DateTime.UtcNow.AddHours(_config.GetValue<int>("Jwt:ExpirationHours", 10));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name,    username),
            new Claim("nombre",           nombre),
            new Claim(ClaimTypes.Role,    rol),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            exp,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
