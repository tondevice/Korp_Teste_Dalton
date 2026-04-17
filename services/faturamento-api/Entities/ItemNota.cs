namespace faturamento_api.Entities;

public class ItemNota
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }

    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }

}
