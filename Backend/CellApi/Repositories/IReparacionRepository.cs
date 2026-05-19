using CellApi.Models;

namespace CellApi.Repositories;

public interface IReparacionRepository
{
    Task<IEnumerable<Reparacion>> GetAllAsync();
    Task<Reparacion?> GetByIdAsync(int id);
    Task<int> CreateAsync(Reparacion reparacion);
    Task UpdateAsync(Reparacion reparacion);
    Task UpdateEstadoAsync(Reparacion reparacion);
    Task<int> AddDetalleAsync(ReparacionDetalle detalle);
    Task RemoveDetalleAsync(int detalleId);
    Task MarcarFacturaEnviadaAsync(int id);
    Task<IEnumerable<Reparacion>> GetHistorialByImeiAsync(string imei, int excluirId);
    Task ActualizarTotalesAsync(int id, decimal baseImponible, decimal porcentajeIva, decimal importeIva, decimal total, decimal precioFinal);
    Task<IEnumerable<ReparacionImagen>> GetImagenesAsync(int reparacionId);
    Task<int> AddImagenAsync(ReparacionImagen imagen);
    Task<int> CountImagenesAsync(int reparacionId);
    Task DeleteImagenAsync(int imagenId);
    Task<Reparacion?> GetByNumeroOrdenAsync(string numeroOrden);
    Task<IEnumerable<Reparacion>> GetReparadasSinRecogerAsync(int diasLimite);
    Task MarcarRecordatorioEnviadoAsync(int id);
}
