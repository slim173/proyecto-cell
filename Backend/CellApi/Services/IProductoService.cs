using CellApi.DTOs;

namespace CellApi.Services;

public interface IProductoService
{
    Task<IEnumerable<ProductoDto>> GetAllAsync(bool soloActivos = true);
    Task<ProductoDto?> GetByIdAsync(int id);
    Task<ProductoDto> CreateAsync(CreateProductoDto dto);
    Task<ProductoDto> UpdateAsync(int id, UpdateProductoDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<CategoriaDto>> GetCategoriasAsync();
    Task<IEnumerable<ProductoDto>> GetStockBajoAsync();
    Task AjustarStockAsync(AjusteStockDto dto);
    Task<ProductoDto?> GetByCodigoAsync(string q);
}
