using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly DbConnectionFactory _db;

    public ProductoRepository(DbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Producto>> GetAllAsync(bool soloActivos = true)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT p.*, c.nombre AS categoria_nombre
            FROM productos p
            LEFT JOIN categorias c ON c.id = p.categoria_id
            WHERE (@SoloActivos = false OR p.activo = true)
            ORDER BY p.nombre";
        return await conn.QueryAsync<Producto>(sql, new { SoloActivos = soloActivos });
    }

    public async Task<Producto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT p.*, c.nombre AS categoria_nombre
            FROM productos p
            LEFT JOIN categorias c ON c.id = p.categoria_id
            WHERE p.id = @Id";
        return await conn.QuerySingleOrDefaultAsync<Producto>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Producto p)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            INSERT INTO productos
                (codigo, nombre, descripcion, categoria_id, precio_venta, costo, stock, stock_minimo, unidad_medida, activo)
            VALUES
                (@Codigo, @Nombre, @Descripcion, @CategoriaId, @PrecioVenta, @Costo, @Stock, @StockMinimo, @UnidadMedida, @Activo)
            RETURNING id";
        return await conn.ExecuteScalarAsync<int>(sql, p);
    }

    public async Task UpdateAsync(Producto p)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            UPDATE productos SET
                codigo             = @Codigo,
                nombre             = @Nombre,
                descripcion        = @Descripcion,
                categoria_id       = @CategoriaId,
                precio_venta       = @PrecioVenta,
                costo              = @Costo,
                stock_minimo       = @StockMinimo,
                unidad_medida      = @UnidadMedida,
                activo             = @Activo,
                fecha_modificacion = NOW()
            WHERE id = @Id";
        await conn.ExecuteAsync(sql, p);
    }

    public async Task UpdateStockAsync(int productoId, int nuevoStock)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE productos SET stock = @Stock, fecha_modificacion = NOW() WHERE id = @Id",
            new { Stock = nuevoStock, Id = productoId });
    }

    public async Task<IEnumerable<Categoria>> GetCategoriasAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Categoria>(
            "SELECT * FROM categorias WHERE activo = true ORDER BY nombre");
    }

    public async Task<Producto?> GetByCodigoAsync(string q)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT p.*, c.nombre AS categoria_nombre
            FROM productos p
            LEFT JOIN categorias c ON c.id = p.categoria_id
            WHERE p.activo = true
              AND (LOWER(p.codigo) = LOWER(@Q) OR p.nombre ILIKE @QLike)
            ORDER BY CASE WHEN LOWER(p.codigo) = LOWER(@Q) THEN 0 ELSE 1 END, p.nombre
            LIMIT 1";
        return await conn.QuerySingleOrDefaultAsync<Producto>(sql,
            new { Q = q, QLike = $"%{q}%" });
    }

    public async Task<IEnumerable<Producto>> GetStockBajoAsync()
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT p.*, c.nombre AS categoria_nombre
            FROM productos p
            LEFT JOIN categorias c ON c.id = p.categoria_id
            WHERE p.activo = true AND p.stock <= p.stock_minimo
            ORDER BY (p.stock_minimo - p.stock) DESC";
        return await conn.QueryAsync<Producto>(sql);
    }
}
