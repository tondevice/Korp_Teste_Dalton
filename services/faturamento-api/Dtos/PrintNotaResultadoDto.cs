namespace faturamento_api.Dtos;

public class PrintNotaResultadoDto
{
    public string Message { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}