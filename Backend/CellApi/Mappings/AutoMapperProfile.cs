using AutoMapper;
using CellApi.DTOs;
using CellApi.Models;

namespace CellApi.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // ── Cliente ──────────────────────────────────────────────
        CreateMap<Cliente, ClienteDto>();
        CreateMap<CreateClienteDto, Cliente>();
        CreateMap<UpdateClienteDto, Cliente>();

        // ── Categoria ────────────────────────────────────────────
        CreateMap<Categoria, CategoriaDto>();

        // ── Proveedor ────────────────────────────────────────────
        CreateMap<Proveedor, ProveedorDto>();
        CreateMap<CreateProveedorDto, Proveedor>();
        CreateMap<UpdateProveedorDto, Proveedor>();

        // ── Producto ─────────────────────────────────────────────
        CreateMap<Producto, ProductoDto>();
        CreateMap<CreateProductoDto, Producto>();
        CreateMap<UpdateProductoDto, Producto>();

        // ── Venta ────────────────────────────────────────────────
        CreateMap<Venta, VentaDto>();
        CreateMap<VentaDetalle, VentaDetalleDto>();

        // ── Reparacion ───────────────────────────────────────────
        CreateMap<Reparacion, ReparacionDto>();
        CreateMap<ReparacionDetalle, ReparacionDetalleDto>();
        CreateMap<ReparacionImagen, ReparacionImagenDto>();

        // ── Compra ───────────────────────────────────────────────
        CreateMap<Compra, CompraDto>();
        CreateMap<CompraDetalle, CompraDetalleDto>();

        // ── Factura ──────────────────────────────────────────────
        CreateMap<Factura, FacturaDto>();

        // ── Inventario ───────────────────────────────────────────
        CreateMap<InventarioMovimiento, InventarioMovimientoDto>();
    }
}
