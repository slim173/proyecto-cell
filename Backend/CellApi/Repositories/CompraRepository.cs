using CellApi.Data;
using CellApi.Models;
using Dapper;

namespace CellApi.Repositories;

public class CompraRepository : ICompraRepository
{
    private readonly DbConnectionFactory _db;

    public CompraRepository(DbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Compra>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT c.*, p.nombre    AS proveedor_nombre,
                        p.telefono AS proveedor_telefono,
                        p.email    AS proveedor_email
            FROM compras c
            JOIN proveedores p ON p.id = c.proveedor_id
            ORDER BY c.fecha DESC";
        return await conn.QueryAsync<Compra>(sql);
    }

    public async Task<Compra?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT c.*, p.nombre    AS proveedor_nombre,
                        p.telefono AS proveedor_telefono,
                        p.email    AS proveedor_email
            FROM compras c
            JOIN proveedores p ON p.id = c.proveedor_id
            WHERE c.id = @Id";

        var compra = await conn.QuerySingleOrDefaultAsync<Compra>(sql, new { Id = id });
        if (compra == null) return null;

        var detalles = await conn.QueryAsync<CompraDetalle>(@"
            SELECT cd.*, pr.nombre AS producto_nombre
            FROM compra_detalles cd
            JOIN productos pr ON pr.id = cd.producto_id
            WHERE cd.compra_id = @Id ORDER BY cd.id",
            new { Id = id });

        compra.Detalles = detalles.ToList();
        return compra;
    }

    public async Task<Compra> CreateAsync(Compra compra, List<CompraDetalle> detalles)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var numero = await conn.ExecuteScalarAsync<string>(
                "SELECT generar_numero_compra()", transaction: tx);
            compra.NumeroCompra = numero!;

            const string sqlCompra = @"
                INSERT INTO compras
                    (numero_compra, proveedor_id, total, estado, observaciones)
                VALUES
                    (@NumeroCompra, @ProveedorId, @Total, @Estado, @Observaciones)
                RETURNING id";

            compra.Id = await conn.ExecuteScalarAsync<int>(sqlCompra, compra, tx);

            foreach (var d in detalles)
            {
                d.CompraId = compra.Id;
                await conn.ExecuteAsync(@"
                    INSERT INTO compra_detalles
                        (compra_id, producto_id, cantidad, costo_unitario, subtotal)
                    VALUES
                        (@CompraId, @ProductoId, @Cantidad, @CostoUnitario, @Subtotal)",
                    d, tx);

                // Actualizar stock e historial
                var stockActual = await conn.ExecuteScalarAsync<int>(
                    "SELECT stock FROM productos WHERE id = @Id FOR UPDATE",
                    new { Id = d.ProductoId }, tx);

                var nuevoStock = stockActual + d.Cantidad;

                await conn.ExecuteAsync(
                    "UPDATE productos SET stock = @Stock, costo = @Costo, fecha_modificacion = NOW() WHERE id = @Id",
                    new { Stock = nuevoStock, Costo = d.CostoUnitario, Id = d.ProductoId }, tx);

                await conn.ExecuteAsync(@"
                    INSERT INTO inventario_movimientos
                        (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, referencia_id, observaciones)
                    VALUES
                        (@ProductoId, 'entrada', @Cantidad, @StockAnterior, @StockPosterior, 'compra', @CompraId, @Obs)",
                    new
                    {
                        ProductoId     = d.ProductoId,
                        Cantidad       = d.Cantidad,
                        StockAnterior  = stockActual,
                        StockPosterior = nuevoStock,
                        CompraId       = compra.Id,
                        Obs            = $"Compra {compra.NumeroCompra}"
                    }, tx);
            }

            // Marcar compra como recibida
            await conn.ExecuteAsync(
                "UPDATE compras SET estado = 'recibida' WHERE id = @Id",
                new { Id = compra.Id }, tx);

            tx.Commit();
            compra.Estado = "recibida";
            compra.Detalles = detalles;
            return compra;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<Proveedor>> GetProveedoresAsync(bool soloActivos = true)
    {
        using var conn = _db.CreateConnection();
        var sql = soloActivos
            ? "SELECT * FROM proveedores WHERE activo = true ORDER BY nombre"
            : "SELECT * FROM proveedores ORDER BY nombre";
        return await conn.QueryAsync<Proveedor>(sql);
    }

    public async Task<Proveedor?> GetProveedorByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Proveedor>(
            "SELECT * FROM proveedores WHERE id = @Id", new { Id = id });
    }

    public async Task<int> CreateProveedorAsync(Proveedor p)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            INSERT INTO proveedores
                (nombre, email, telefono, direccion, ciudad, codigo_postal, cif, activo, observaciones)
            VALUES
                (@Nombre, @Email, @Telefono, @Direccion, @Ciudad, @CodigoPostal, @Cif, @Activo, @Observaciones)
            RETURNING id";
        return await conn.ExecuteScalarAsync<int>(sql, p);
    }

    public async Task UpdateProveedorAsync(Proveedor p)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            UPDATE proveedores SET
                nombre             = @Nombre,
                email              = @Email,
                telefono           = @Telefono,
                direccion          = @Direccion,
                ciudad             = @Ciudad,
                codigo_postal      = @CodigoPostal,
                cif                = @Cif,
                activo             = @Activo,
                observaciones      = @Observaciones,
                fecha_modificacion = NOW()
            WHERE id = @Id";
        await conn.ExecuteAsync(sql, p);
    }
}
