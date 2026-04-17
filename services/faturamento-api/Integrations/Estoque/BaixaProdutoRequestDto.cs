namespace faturamento_api.Integrations.Estoque;

public class BaixaProdutoRequestDto
{
    public List<BaixaProdutoDto> Items { get; set; } = [];
}
