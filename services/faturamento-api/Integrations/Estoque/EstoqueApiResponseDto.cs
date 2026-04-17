namespace faturamento_api.Integrations.Estoque;

public class EstoqueApiResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool IsClientError => StatusCode >= 400 && StatusCode < 500;
    public bool IsServerError => StatusCode >= 500;
}