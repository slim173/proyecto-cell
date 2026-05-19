namespace CellApp.Models;

// ── Auth ────────────────────────────────────────────────────────
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token      { get; set; } = string.Empty;
    public string Username   { get; set; } = string.Empty;
    public string Nombre     { get; set; } = string.Empty;
    public string Rol        { get; set; } = "empleado";
    public DateTime Expiration { get; set; }
}

public class UsuarioAdminDto
{
    public int     Id            { get; set; }
    public string  Username      { get; set; } = string.Empty;
    public string  Nombre        { get; set; } = string.Empty;
    public string? Email         { get; set; }
    public string  Rol           { get; set; } = "empleado";
    public bool    Activo        { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateUsuarioDto
{
    public string  Username  { get; set; } = string.Empty;
    public string  Nombre    { get; set; } = string.Empty;
    public string  Password  { get; set; } = string.Empty;
    public string? Email     { get; set; }
    public string  Rol       { get; set; } = "empleado";
}

public class UpdateUsuarioAdminDto
{
    public string  Nombre       { get; set; } = string.Empty;
    public string? Email        { get; set; }
    public string  Rol          { get; set; } = "empleado";
    public bool    Activo       { get; set; } = true;
    public string? PasswordNueva { get; set; }
}

public class PerfilDto
{
    public string  Username          { get; set; } = string.Empty;
    public string  Nombre            { get; set; } = string.Empty;
    public string? Email             { get; set; }
    public bool    TieneSmtpPassword { get; set; }
}

public class UpdatePerfilDto
{
    public string  Nombre             { get; set; } = string.Empty;
    public string? Email              { get; set; }
    public string? PasswordActual     { get; set; }
    public string? PasswordNueva      { get; set; }
    public string? SmtpPasswordNueva  { get; set; }
}

// ── Empresa / Configuración ─────────────────────────────────────
public class EmpresaDto
{
    public string  Nombre            { get; set; } = string.Empty;
    public string? Cif               { get; set; }
    public string? Direccion         { get; set; }
    public string? Ciudad            { get; set; }
    public string? CodigoPostal      { get; set; }
    public string? Telefono          { get; set; }
    public string? Email             { get; set; }
    public string? Web               { get; set; }
    public string? PieFactura        { get; set; }
    public decimal IvaPorcentaje     { get; set; } = 21;
    public string? SmtpHost          { get; set; }
    public int     SmtpPuerto        { get; set; } = 587;
    public bool    SmtpSsl           { get; set; } = true;
    public string? SmtpUsuario       { get; set; }
    public string? SmtpFromName      { get; set; }
    public string? SmtpFromEmail     { get; set; }
    public bool    TieneSmtpPassword { get; set; }
    public string? WaMsgEntrada      { get; set; }
    public string? WaMsgListo        { get; set; }
    public string? WaMsgRecordatorio { get; set; }
    // WhatsApp API / Twilio
    public bool    WhatsappActivo       { get; set; }
    public string? TwilioAccountSid    { get; set; }
    public bool    TieneTwilioAuthToken { get; set; }
    public string? TwilioWhatsappFrom  { get; set; }
    public bool    RecordatorioActivo  { get; set; }
    public int     RecordatorioDias    { get; set; } = 3;
    // Print settings
    public string  TicketFormato      { get; set; } = "a4";
    public string? ClausulaReparacion { get; set; }
    public string? ClausulaRecogida   { get; set; }
    public bool    TicketMostrarQr    { get; set; } = true;
    public string? LogoUrl            { get; set; }
    public string? UrlPublica         { get; set; }
}

public class UpdateEmpresaDto
{
    public string  Nombre          { get; set; } = string.Empty;
    public string? Cif             { get; set; }
    public string? Direccion       { get; set; }
    public string? Ciudad          { get; set; }
    public string? CodigoPostal    { get; set; }
    public string? Telefono        { get; set; }
    public string? Email           { get; set; }
    public string? Web             { get; set; }
    public string? PieFactura      { get; set; }
    public decimal IvaPorcentaje   { get; set; } = 21;
    public string? SmtpHost        { get; set; }
    public int     SmtpPuerto      { get; set; } = 587;
    public bool    SmtpSsl         { get; set; } = true;
    public string? SmtpUsuario     { get; set; }
    public string? SmtpPassword    { get; set; }  // null = no cambiar
    public string? SmtpFromName    { get; set; }
    public string? SmtpFromEmail   { get; set; }
    public string? WaMsgEntrada      { get; set; }
    public string? WaMsgListo        { get; set; }
    public string? WaMsgRecordatorio { get; set; }
    // WhatsApp API / Twilio
    public bool    WhatsappActivo       { get; set; }
    public string? TwilioAccountSid    { get; set; }
    public string? TwilioAuthToken     { get; set; }
    public string? TwilioWhatsappFrom  { get; set; }
    public bool    RecordatorioActivo  { get; set; }
    public int     RecordatorioDias    { get; set; } = 3;
    // Print settings
    public string  TicketFormato      { get; set; } = "a4";
    public string? ClausulaReparacion { get; set; }
    public string? ClausulaRecogida   { get; set; }
    public bool    TicketMostrarQr    { get; set; } = true;
    public string? UrlPublica         { get; set; }
}

// ── Respuesta estándar API ──────────────────────────────────────
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T?   Data    { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

// ── Clientes ────────────────────────────────────────────────────
public class ClienteDto
{
    public int Id { get; set; }
    public string Nombre   { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();
    public string Email    { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Nif { get; set; }
    public bool Activo { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateClienteDto
{
    public string Nombre   { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string Email    { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Nif { get; set; }
    public string? Observaciones { get; set; }
}

public class UpdateClienteDto : CreateClienteDto
{
    public bool Activo { get; set; } = true;
}

// ── Categorías ──────────────────────────────────────────────────
public class CategoriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}

// ── Productos ───────────────────────────────────────────────────
public class ProductoDto
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Costo { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public bool StockBajo => Stock <= StockMinimo;
    public string UnidadMedida { get; set; } = "unidad";
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateProductoDto
{
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? CategoriaId { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Costo { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public string UnidadMedida { get; set; } = "unidad";
}

public class UpdateProductoDto : CreateProductoDto
{
    public bool Activo { get; set; } = true;
}

public class AjusteStockDto
{
    public int ProductoId { get; set; }
    public int NuevoStock { get; set; }
    public string? Observaciones { get; set; }
}

// ── Ventas ──────────────────────────────────────────────────────
public class VentaDto
{
    public int Id { get; set; }
    public string NumeroVenta { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteApellidos { get; set; }
    public string? ClienteNombreCompleto => $"{ClienteNombre} {ClienteApellidos}".Trim();
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }
    public string? ClienteNif { get; set; }
    public DateTime Fecha { get; set; }
    public decimal BaseImponible  { get; set; }
    public decimal PorcentajeIva  { get; set; }
    public decimal ImporteIva     { get; set; }
    public decimal Descuento      { get; set; }
    public string  TipoDescuento  { get; set; } = "importe";
    public decimal Total          { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MetodoPago { get; set; }
    public string? Observaciones { get; set; }
    public bool FacturaEnviada { get; set; }
    public string? NumeroFactura { get; set; }
    public List<VentaDetalleDto> Detalles { get; set; } = new();
}

public class VentaDetalleDto
{
    public int Id { get; set; }
    public int? ProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreateVentaDto
{
    public int ClienteId { get; set; }
    public string MetodoPago { get; set; } = "efectivo";
    public string? Observaciones { get; set; }
    public decimal Descuento { get; set; }
    public string TipoDescuento { get; set; } = "importe";
    public List<CreateVentaDetalleDto> Detalles { get; set; } = new();
}

public class CreateVentaDetalleDto
{
    public int? ProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Math.Round(Cantidad * PrecioUnitario, 2);
}

// ── Reparaciones ────────────────────────────────────────────────
public class ReparacionDto
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteApellidos { get; set; }
    public string? ClienteNombreCompleto => $"{ClienteNombre} {ClienteApellidos}".Trim();
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }
    public string Dispositivo { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Imei { get; set; }
    public string DescripcionFalla { get; set; } = string.Empty;
    public string? ObservacionesTecnico { get; set; }
    public string? Solucion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public decimal? PrecioEstimado { get; set; }
    public decimal? PrecioFinal { get; set; }
    public decimal? BaseImponible { get; set; }
    public decimal PorcentajeIva { get; set; }
    public decimal? ImporteIva { get; set; }
    public decimal? Total { get; set; }
    public string? TecnicoAsignado { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public DateTime? FechaEstimadaEntrega { get; set; }
    public DateTime? FechaEntregaReal { get; set; }
    public bool FacturaEnviada { get; set; }
    public List<ReparacionDetalleDto> Detalles { get; set; } = new();
    public List<ReparacionImagenDto>  Imagenes { get; set; } = new();
}

public class ReparacionImagenDto
{
    public int Id { get; set; }
    public int ReparacionId { get; set; }
    public string RutaImagen { get; set; } = string.Empty;
    public string? NombreArchivo { get; set; }
    public DateTime Fecha { get; set; }
}

public class ReparacionDetalleDto
{
    public int Id { get; set; }
    public int? ProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreateReparacionDto
{
    public int ClienteId { get; set; }
    public string Dispositivo { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Imei { get; set; }
    public string DescripcionFalla { get; set; } = string.Empty;
    public string Prioridad { get; set; } = "normal";
    public decimal? PrecioEstimado { get; set; }
    public string? TecnicoAsignado { get; set; }
    public DateTime? FechaEstimadaEntrega { get; set; }
}

public class UpdateReparacionDto
{
    public int ClienteId { get; set; }
    public string Dispositivo { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Imei { get; set; }
    public string DescripcionFalla { get; set; } = string.Empty;
    public string Prioridad { get; set; } = "normal";
    public decimal? PrecioEstimado { get; set; }
    public string? TecnicoAsignado { get; set; }
    public DateTime? FechaEstimadaEntrega { get; set; }
}

public class UpdateReparacionEstadoDto
{
    public string Estado { get; set; } = string.Empty;
    public string? ObservacionesTecnico { get; set; }
    public string? Solucion { get; set; }
    public decimal? PrecioEstimado { get; set; }
    public decimal? PrecioFinal { get; set; }
    public string? TecnicoAsignado { get; set; }
    public DateTime? FechaEstimadaEntrega { get; set; }
}

public class HistorialEquipoDto
{
    public int      Id              { get; set; }
    public string   NumeroOrden     { get; set; } = string.Empty;
    public string   Estado          { get; set; } = string.Empty;
    public DateTime FechaRecepcion  { get; set; }
    public DateTime? FechaEntregaReal { get; set; }
    public string   DescripcionFalla { get; set; } = string.Empty;
    public string?  Solucion        { get; set; }
    public string?  TecnicoAsignado { get; set; }
    public decimal? Total           { get; set; }
}

public class AddReparacionDetalleDto
{
    public int? ProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
}

// ── Proveedores + Compras ────────────────────────────────────────
public class ProveedorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Cif { get; set; }
    public bool Activo { get; set; }
}

public class CreateProveedorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Cif { get; set; }
    public string? Observaciones { get; set; }
}

public class CompraDto
{
    public int Id { get; set; }
    public string NumeroCompra { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ProveedorTelefono { get; set; }
    public string? ProveedorEmail { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<CompraDetalleDto> Detalles { get; set; } = new();
}

public class CompraDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? ProductoNombre { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreateCompraDto
{
    public int ProveedorId { get; set; }
    public string? Observaciones { get; set; }
    public List<CreateCompraDetalleDto> Detalles { get; set; } = new();
}

public class CreateCompraDetalleDto
{
    public int ProductoId { get; set; }
    public string? ProductoNombre { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal => Math.Round(Cantidad * CostoUnitario, 2);
}

// ── Facturas ────────────────────────────────────────────────────
public class FacturaDto
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public int? VentaId { get; set; }
    public int? ReparacionId { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteApellidos { get; set; }
    public string? ClienteNombreCompleto => $"{ClienteNombre} {ClienteApellidos}".Trim();
    public string? ClienteEmail { get; set; }
    public string? ClienteNif { get; set; }
    public DateTime FechaEmision { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal PorcentajeIva { get; set; }
    public decimal ImporteIva { get; set; }
    public decimal Total { get; set; }
    public string? PdfPath { get; set; }
    public bool Anulada { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateFacturaDto
{
    public int ClienteId { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.Today;
    public List<CreateFacturaLineaDto> Lineas { get; set; } = new();
}

public class CreateFacturaLineaDto
{
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
}

public class CrearFacturaResponse
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
}

// ── Inventario ──────────────────────────────────────────────────
public class InventarioMovimientoDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? ProductoCodigo { get; set; }
    public string? ProductoNombre { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockPosterior { get; set; }
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
    public string? Observaciones { get; set; }
    public string? Usuario { get; set; }
    public DateTime Fecha { get; set; }
}

public class ProductoStockBajoDto
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public int UnidadesFaltantes { get; set; }
    public string? Categoria { get; set; }
}

// ── Dashboard ───────────────────────────────────────────────────
public class DashboardDto
{
    public ResumenVentasDto VentasHoy   { get; set; } = new();
    public ResumenVentasDto VentasMes   { get; set; } = new();
    public ResumenVentasDto VentasAnio  { get; set; } = new();
    public int ReparacionesAbiertas       { get; set; }
    public int ReparacionesEntregadasHoy  { get; set; }
    public int ProductosStockBajo         { get; set; }
    public List<VentaResumenDto>        UltimasVentas       { get; set; } = new();
    public List<ReparacionResumenDto>   UltimasReparaciones { get; set; } = new();
    public List<ProductoStockBajoDto>   AlertasStock        { get; set; } = new();
    public List<DiaVentasDto>           VentasUltimos7Dias  { get; set; } = new();
    public List<EstadoReparacionDto>    ReparacionesPorEstado { get; set; } = new();
}

public class DiaVentasDto
{
    public string  Fecha    { get; set; } = string.Empty;
    public decimal Total    { get; set; }
    public int     Cantidad { get; set; }
}

public class EstadoReparacionDto
{
    public string Estado    { get; set; } = string.Empty;
    public int    Cantidad  { get; set; }
}

public class ResumenVentasDto
{
    public int TotalVentas   { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal IvaTotal  { get; set; }
    public decimal TicketMedio { get; set; }
}

public class VentaResumenDto
{
    public int Id { get; set; }
    public string NumeroVenta { get; set; } = string.Empty;
    public string? ClienteNombre { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MetodoPago { get; set; }
}

// ── Descuento en VentaDto ────────────────────────────────────────
// (añadido a la clase VentaDto existente via partial — ver abajo)

public class VentaConDescuentoExt
{
    public decimal Descuento     { get; set; }
    public string  TipoDescuento { get; set; } = "importe";
}

public class ReparacionResumenDto
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public string? ClienteNombre { get; set; }
    public string Dispositivo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public DateTime FechaRecepcion { get; set; }
}

// ════════════════════════════════════════════════════════════════
// GARANTÍAS
// ════════════════════════════════════════════════════════════════
public class GarantiaDto
{
    public int      Id                    { get; set; }
    public string?  NumeroGarantia        { get; set; }
    public string   Tipo                  { get; set; } = "";
    public int      ReferenciaId          { get; set; }
    public int      ClienteId             { get; set; }
    public string?  ClienteNombreCompleto { get; set; }
    public string?  ClienteTelefono       { get; set; }
    public string   ProductoDescripcion   { get; set; } = "";
    public DateTime FechaInicio           { get; set; }
    public DateTime FechaFin              { get; set; }
    public int      Meses                 { get; set; }
    public string   Estado                { get; set; } = "";
    public string?  Observaciones         { get; set; }
    public DateTime FechaCreacion         { get; set; }
    public bool     Vencida               => DateTime.UtcNow > FechaFin;
    public int      DiasRestantes         => (int)(FechaFin - DateTime.UtcNow).TotalDays;
}

public class CreateGarantiaDto
{
    public string   Tipo                { get; set; } = "venta";
    public int      ReferenciaId        { get; set; }
    public int      ClienteId           { get; set; }
    public string   ProductoDescripcion { get; set; } = "";
    public DateTime FechaInicio         { get; set; } = DateTime.Today;
    public int      Meses               { get; set; } = 12;
    public string?  Observaciones       { get; set; }
}

// ════════════════════════════════════════════════════════════════
// CAJA / TPV
// ════════════════════════════════════════════════════════════════
public class CajaSesionDto
{
    public int       Id               { get; set; }
    public string?   NumeroSesion     { get; set; }
    public DateTime  FechaApertura    { get; set; }
    public DateTime? FechaCierre      { get; set; }
    public decimal   EfectivoApertura { get; set; }
    public decimal?  EfectivoCierre   { get; set; }
    public decimal   TotalEfectivo    { get; set; }
    public decimal   TotalTarjeta     { get; set; }
    public decimal   TotalOtros       { get; set; }
    public decimal?  Diferencia       { get; set; }
    public string    Estado           { get; set; } = "";
    public string?   UsuarioApertura  { get; set; }
    public string?   UsuarioCierre    { get; set; }
    public string?   Observaciones    { get; set; }
    public DateTime  FechaCreacion    { get; set; }
    public List<CajaMovimientoDto> Movimientos { get; set; } = new();
    public decimal TotalVentas => TotalEfectivo + TotalTarjeta + TotalOtros;
}

public class CajaMovimientoDto
{
    public int      Id             { get; set; }
    public int      SesionId       { get; set; }
    public string   Tipo           { get; set; } = "";
    public string   Concepto       { get; set; } = "";
    public decimal  Importe        { get; set; }
    public string?  MetodoPago     { get; set; }
    public string?  ReferenciaTipo { get; set; }
    public int?     ReferenciaId   { get; set; }
    public string?  Usuario        { get; set; }
    public DateTime Fecha          { get; set; }
}

// ════════════════════════════════════════════════════════════════
// WHATSAPP
// ════════════════════════════════════════════════════════════════
public class EnviarWhatsAppDto
{
    public int    ClienteId { get; set; }
    public string Mensaje   { get; set; } = string.Empty;
}

public class EnviarWhatsAppMasivoDto
{
    public List<int> ClienteIds { get; set; } = new();
    public string    Mensaje    { get; set; } = string.Empty;
}

public class WhatsAppResultadoDto
{
    public int    ClienteId     { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string Telefono      { get; set; } = string.Empty;
    public bool   Ok            { get; set; }
    public string Mensaje       { get; set; } = string.Empty;
}

public class WhatsAppLogDto
{
    public int      Id             { get; set; }
    public string   Destinatario   { get; set; } = string.Empty;
    public int?     ClienteId      { get; set; }
    public string   MensajeResumen { get; set; } = string.Empty;
    public string   Estado         { get; set; } = string.Empty;
    public DateTime FechaCreacion  { get; set; }
}
