using CellApi.DTOs;
using CellApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CellApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    public AuthController(IAuthService service) => _service = service;

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var result = await _service.LoginAsync(dto);
        if (result == null)
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Usuario o contraseña incorrectos."));

        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Bienvenido/a al sistema."));
    }
}
