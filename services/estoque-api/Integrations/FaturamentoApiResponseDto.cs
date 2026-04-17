namespace estoque_api.Integrations.Faturamento;

public class FaturamentoApiResponseDto
{
    public bool Linked { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InvoicesCount { get; set; }
}