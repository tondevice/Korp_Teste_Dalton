namespace faturamento_api.Entities;

public class Nota
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Status { get; set; } = "Aberta";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ItemNota> Items { get; set; } = [];
}
