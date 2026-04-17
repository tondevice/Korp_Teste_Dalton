using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace faturamento_api.Integrations.Estoque;

public class EstoqueApiClient
{
    private readonly HttpClient _httpClient;

    public EstoqueApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EstoqueApiResponseDto> DecreaseStockAsync(BaixaProdutoRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/produtos/baixas", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EstoqueApiResponseDto>();

                return result ?? new EstoqueApiResponseDto
                {
                    Success = true,
                    StatusCode = (int)response.StatusCode,
                    Message = "Estoque atualizado com sucesso."
                };
            }

            var rawContent = await response.Content.ReadAsStringAsync();
            var parsedMessage = ExtrairMensagem(rawContent);

            return new EstoqueApiResponseDto
            {
                Success = false,
                StatusCode = (int)response.StatusCode,
                Message = string.IsNullOrWhiteSpace(parsedMessage)
                    ? response.StatusCode == HttpStatusCode.BadRequest
                        ? "Não foi possível validar o estoque para concluir a impressão."
                        : "O serviço de estoque retornou um erro ao processar a solicitação."
                    : parsedMessage
            };
        }
        catch (TaskCanceledException)
        {
            return new EstoqueApiResponseDto
            {
                Success = false,
                StatusCode = StatusCodes.Status504GatewayTimeout,
                Message = "A impressão não foi concluída porque o serviço de estoque demorou para responder. A nota continua em aberto. Tente novamente em instantes."
            };
        }
        catch (HttpRequestException)
        {
            return new EstoqueApiResponseDto
            {
                Success = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                Message = "A impressão não foi concluída porque o serviço de estoque está indisponível no momento. A nota continua em aberto. Tente novamente em instantes."
            };
        }
        catch
        {
            return new EstoqueApiResponseDto
            {
                Success = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "A impressão não foi concluída por uma falha na comunicação com o serviço de estoque. A nota continua em aberto."
            };
        }
    }

    private static string ExtrairMensagem(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return string.Empty;

        try
        {
            var json = JsonNode.Parse(rawContent);

            if (json is JsonObject obj)
            {
                var details = obj["details"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(details))
                    return details;

                var message = obj["message"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(message))
                    return message;

                var title = obj["title"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(title))
                    return title;
            }
        }
        catch (JsonException)
        {
        }

        return rawContent;
    }
}
