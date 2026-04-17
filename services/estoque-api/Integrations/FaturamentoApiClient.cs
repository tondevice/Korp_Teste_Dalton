using System.Net.Http.Json;

namespace estoque_api.Integrations.Faturamento;

public class FaturamentoApiClient
{
    private readonly HttpClient _httpClient;

    public FaturamentoApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FaturamentoApiResponseDto> CheckProductLinkAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/notas/produtos/{productId}/vinculo");

            if (!response.IsSuccessStatusCode)
            {
                return new FaturamentoApiResponseDto
                {
                    Linked = true,
                    Message = "Não foi possível confirmar se o produto está vinculado a notas fiscais.",
                    InvoicesCount = 0
                };
            }

            var result = await response.Content.ReadFromJsonAsync<FaturamentoApiResponseDto>();

            return result ?? new FaturamentoApiResponseDto
            {
                Linked = true,
                Message = "Não foi possível confirmar se o produto está vinculado a notas fiscais.",
                InvoicesCount = 0
            };
        }
        catch
        {
            return new FaturamentoApiResponseDto
            {
                Linked = true,
                Message = "Não foi possível confirmar o vínculo do produto porque o serviço de faturamento está indisponível no momento.",
                InvoicesCount = 0
            };
        }
    }
}
