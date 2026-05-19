namespace CellApi.DTOs;

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
    public decimal   TotalVentas      => TotalEfectivo + TotalTarjeta + TotalOtros;
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

public class AbrirCajaDto
{
    public decimal EfectivoApertura { get; set; }
    public string? Observaciones    { get; set; }
}

public class CerrarCajaDto
{
    public decimal EfectivoCierre { get; set; }
    public string? Observaciones  { get; set; }
}

public class AddMovimientoCajaDto
{
    public string   Tipo       { get; set; } = "entrada";
    public string   Concepto   { get; set; } = "";
    public decimal  Importe    { get; set; }
    public string?  MetodoPago { get; set; }
}
