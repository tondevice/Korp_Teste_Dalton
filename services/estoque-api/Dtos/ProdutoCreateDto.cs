namespace estoque_api.Dtos;

public class ProdutoCreateDto
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Stock { get; set; }
}
