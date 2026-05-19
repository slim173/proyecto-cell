namespace CellApi.Services;

public interface IEmailService
{
    Task SendAsync(
        string destinatario,
        string asunto,
        string cuerpo,
        string tipo,
        string referenciaTipo,
        int referenciaId,
        byte[]? adjuntoPdf = null,
        string? nombreAdjunto = null);
}
