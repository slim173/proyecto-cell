namespace CellApi.Models;

public class Garantia
{
    public int      Id                   { get; set; }
    public string?  NumeroGarantia       { get; set; }
    public string   Tipo                 { get; set; } = "venta";
    public int      ReferenciaId         { get; set; }
    public int      ClienteId            { get; set; }
    public string   ProductoDescripcion  { get; set; } = "";
    public DateTime FechaInicio          { get; set; }
    public DateTime FechaFin             { get; set; }
    public int      Meses                { get; set; } = 12;
    public string   Estado               { get; set; } = "activa";
    public string?  Observaciones        { get; set; }
    public DateTime FechaCreacion        { get; set; }
    // navegación
    public string?  ClienteNombre        { get; set; }
    public string?  ClienteApellidos     { get; set; }
    public string?  ClienteTelefono      { get; set; }
}
