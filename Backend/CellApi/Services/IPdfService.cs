using CellApi.DTOs;

namespace CellApi.Services;

public interface IPdfService
{
    Task<string>  GenerarFacturaPdfAsync(FacturaDto factura);
    Task<byte[]>  GenerarFacturaPdfBytesAsync(FacturaDto factura, string? formatoOverride = null);
    Task<byte[]>  GenerarOrdenReparacionPdfAsync(ReparacionDto rep, string? formatoOverride = null);
    Task<byte[]>  GenerarTicketVentaPdfAsync(VentaDto venta, string? formatoOverride = null);
    Task<byte[]>  GenerarOrdenCompraPdfAsync(CompraDto compra);
    Task<byte[]>  GenerarEtiquetaReparacionPdfAsync(ReparacionDto rep);
    Task<byte[]>  GenerarEtiquetasPrecioPdfAsync(IEnumerable<ProductoDto> productos, string formato = "50x30");
    byte[]        GenerarQrPng(string contenido, int px);
}
