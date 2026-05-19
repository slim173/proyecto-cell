using CellApi.DTOs;
using CellApi.Models;
using CellApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioRepository       _repo;
    private readonly IConfiguracionRepository _configRepo;

    public UsuariosController(IUsuarioRepository repo, IConfiguracionRepository configRepo)
    {
        _repo       = repo;
        _configRepo = configRepo;
    }

    // ── Perfil propio ────────────────────────────────────────────

    [HttpGet("perfil")]
    public async Task<ActionResult<ApiResponse<PerfilDto>>> GetPerfil()
    {
        var username = User.Identity?.Name;
        if (username == null) return Unauthorized();

        var usuario = await _repo.GetByUsernameAsync(username);
        if (usuario == null) return NotFound(ApiResponse<PerfilDto>.Fail("Usuario no encontrado."));

        var smtpPwd = await _configRepo.GetValorAsync("smtp_password");
        return Ok(ApiResponse<PerfilDto>.Ok(new PerfilDto
        {
            Username          = usuario.Username,
            Nombre            = usuario.Nombre,
            Email             = usuario.Email,
            TieneSmtpPassword = !string.IsNullOrWhiteSpace(smtpPwd)
        }));
    }

    [HttpPut("perfil")]
    public async Task<ActionResult<ApiResponse<PerfilDto>>> UpdatePerfil([FromBody] UpdatePerfilDto dto)
    {
        var username = User.Identity?.Name;
        if (username == null) return Unauthorized();

        var usuario = await _repo.GetByUsernameAsync(username);
        if (usuario == null) return NotFound(ApiResponse<PerfilDto>.Fail("Usuario no encontrado."));

        string? nuevoHash = null;
        if (!string.IsNullOrWhiteSpace(dto.PasswordNueva))
        {
            if (string.IsNullOrWhiteSpace(dto.PasswordActual))
                return BadRequest(ApiResponse<PerfilDto>.Fail("Debes ingresar la contraseña actual para cambiarla."));
            if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
                return BadRequest(ApiResponse<PerfilDto>.Fail("La contraseña actual no es correcta."));
            if (dto.PasswordNueva.Length < 6)
                return BadRequest(ApiResponse<PerfilDto>.Fail("La nueva contraseña debe tener al menos 6 caracteres."));
            nuevoHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva, 12);
        }

        await _repo.UpdatePerfilAsync(username, dto.Nombre, dto.Email, nuevoHash);

        if (!string.IsNullOrWhiteSpace(dto.SmtpPasswordNueva))
            await _configRepo.SetValorAsync("smtp_password", dto.SmtpPasswordNueva);

        return Ok(ApiResponse<PerfilDto>.Ok(new PerfilDto
        {
            Username          = username,
            Nombre            = dto.Nombre,
            Email             = dto.Email,
            TieneSmtpPassword = !string.IsNullOrWhiteSpace(dto.SmtpPasswordNueva)
                                || !string.IsNullOrWhiteSpace(await _configRepo.GetValorAsync("smtp_password"))
        }, "Perfil actualizado correctamente."));
    }

    // ── Gestión de usuarios (solo admin) ─────────────────────────

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UsuarioAdminDto>>>> GetAll()
    {
        var lista = await _repo.GetAllAsync();
        var dtos  = lista.Select(u => new UsuarioAdminDto
        {
            Id            = u.Id,
            Username      = u.Username,
            Nombre        = u.Nombre,
            Email         = u.Email,
            Rol           = u.Rol,
            Activo        = u.Activo,
            FechaCreacion = u.FechaCreacion
        });
        return Ok(ApiResponse<IEnumerable<UsuarioAdminDto>>.Ok(dtos));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<UsuarioAdminDto>>> Create([FromBody] CreateUsuarioDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<UsuarioAdminDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        if (dto.Password.Length < 6)
            return BadRequest(ApiResponse<UsuarioAdminDto>.Fail("La contraseña debe tener al menos 6 caracteres."));

        var nuevo = new Usuario
        {
            Username     = dto.Username.Trim().ToLower(),
            Nombre       = dto.Nombre.Trim(),
            Email        = dto.Email?.Trim(),
            Rol          = dto.Rol,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 12),
            Activo       = true
        };

        try
        {
            var id = await _repo.CreateAsync(nuevo);
            return Ok(ApiResponse<UsuarioAdminDto>.Ok(new UsuarioAdminDto
            {
                Id = id, Username = nuevo.Username, Nombre = nuevo.Nombre,
                Email = nuevo.Email, Rol = nuevo.Rol, Activo = true,
                FechaCreacion = DateTime.UtcNow
            }, "Usuario creado correctamente."));
        }
        catch (Exception ex) when (ex.Message.Contains("unique") || ex.Message.Contains("duplicate")
                                   || ex.Message.Contains("23505"))
        {
            return Conflict(ApiResponse<UsuarioAdminDto>.Fail("El nombre de usuario ya existe."));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<UsuarioAdminDto>>> Update(int id, [FromBody] UpdateUsuarioAdminDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<UsuarioAdminDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var usuario = await _repo.GetByIdAsync(id);
        if (usuario == null) return NotFound(ApiResponse<UsuarioAdminDto>.Fail("Usuario no encontrado."));

        if (usuario.Rol == "admin" && dto.Rol != "admin")
        {
            var todos  = await _repo.GetAllAsync();
            var admins = todos.Count(u => u.Rol == "admin" && u.Activo && u.Id != id);
            if (admins == 0)
                return BadRequest(ApiResponse<UsuarioAdminDto>.Fail("Debe haber al menos un administrador activo."));
        }

        usuario.Nombre       = dto.Nombre.Trim();
        usuario.Email        = dto.Email?.Trim();
        usuario.Rol          = dto.Rol;
        usuario.Activo       = dto.Activo;
        usuario.PasswordHash = !string.IsNullOrWhiteSpace(dto.PasswordNueva)
            ? BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva, 12)
            : string.Empty;

        await _repo.UpdateAdminAsync(usuario);

        return Ok(ApiResponse<UsuarioAdminDto>.Ok(new UsuarioAdminDto
        {
            Id = usuario.Id, Username = usuario.Username, Nombre = usuario.Nombre,
            Email = usuario.Email, Rol = usuario.Rol, Activo = usuario.Activo,
            FechaCreacion = usuario.FechaCreacion
        }, "Usuario actualizado correctamente."));
    }
}
