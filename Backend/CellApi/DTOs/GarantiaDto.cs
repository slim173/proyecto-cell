namespace CellApi.DTOs;

public class GarantiaDto
{
    public int      Id                   { get; set; }
    public string?  NumeroGarantia       { get; set; }
    public string   Tipo                 { get; set; } = "";
    public int      ReferenciaId         { get; set; }
    public int      ClienteId            { get; set; }
    public string?  ClienteNombreCompleto { get; set; }
    public string?  ClienteTelefono      { get; set; }
    public string   ProductoDescripcion  { get; set; } = "";
    public DateTime FechaInicio          { get; set; }
    public DateTime FechaFin             { get; set; }
    public int      Meses                { get; set; }
    public string   Estado               { get; set; } = "";
    public string?  Observaciones        { get; set; }
    public DateTime FechaCreacion        { get; set; }
    public bool     Vencida              => DateTime.UtcNow > FechaFin;
    public int      DiasRestantes        => (int)(FechaFin - DateTime.UtcNow).TotalDays;
}

public class CreateGarantiaDto
{
    public string   Tipo                { get; set; } = "venta";
    public int      ReferenciaId        { get; set; }
    public int      ClienteId           { get; set; }
    public string   ProductoDescripcion { get; set; } = "";
    public DateTime FechaInicio         { get; set; }
    public int      Meses               { get; set; } = 12;
    public string?  Observaciones       { get; set; }
}

public class UpdateGarantiaEstadoDto
{
    public string Estado { get; set; } = "";
}
