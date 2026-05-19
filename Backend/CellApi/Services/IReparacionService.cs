using CellApi.DTOs;

namespace CellApi.Services;

public interface IReparacionService
{
    Task<IEnumerable<ReparacionDto>> GetAllAsync();
    Task<ReparacionDto?> GetByIdAsync(int id);
    Task<ReparacionDto> CreateAsync(CreateReparacionDto dto);
    Task<ReparacionDto> UpdateAsync(int id, UpdateReparacionDto dto);
    Task<ReparacionDto> UpdateEstadoAsync(int id, UpdateReparacionEstadoDto dto);
    Task<ReparacionDetalleDto> AddDetalleAsync(int id, AddReparacionDetalleDto dto);
    Task RemoveDetalleAsync(int reparacionId, int detalleId);
    Task EnviarNotificacionAsync(int id);
    Task<IEnumerable<HistorialEquipoDto>> GetHistorialEquipoAsync(int id);
    Task<ReparacionDto?> GetByNumeroOrdenAsync(string numeroOrden);
    Task<IEnumerable<ReparacionDto>> GetReparadasSinRecogerAsync(int diasLimite);
    Task MarcarRecordatorioEnviadoAsync(int id);
}
